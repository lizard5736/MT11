using System.Text;

namespace Grip.Core.Text;

public sealed class UrlCleanerOptions
{
    public bool UnwrapRedirects { get; set; } = true;
    public bool CleanSearchLinks { get; set; } = true;
    public IReadOnlyCollection<string> ExtraParameters { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<string> SkipDomains { get; set; } = Array.Empty<string>();
}

public sealed record UrlCleanResult(string Url, int RemovedCount, bool Unwrapped, IReadOnlyList<string> RemovedNames)
{
    public bool Changed => RemovedCount > 0 || Unwrapped;
}

/// <summary>
/// Strips tracking parameters from links and unwraps well-known redirect
/// wrappers. Only the query is rebuilt; everything else in the link is kept
/// byte for byte, so a cleaned link never breaks.
/// </summary>
public static class UrlCleaner
{
    // Tracking parameters removed everywhere (compared case-insensitively).
    private static readonly HashSet<string> GlobalParams = new(StringComparer.OrdinalIgnoreCase)
    {
        "fbclid", "gclid", "gclsrc", "dclid", "gbraid", "wbraid", "gad_source", "msclkid", "twclid", "ttclid",
        "li_fat_id", "yclid", "ysclid", "_openstat", "igshid", "igsh", "mc_cid", "mc_eid", "mkt_tok",
        "_hsenc", "_hsmi", "__hssc", "__hstc", "__hsfp", "hsctatracking", "_ga", "_gl", "wt.mc_id", "wt_mc_id",
        "s_cid", "srsltid", "_branch_match_id", "_branch_referrer", "epik", "vero_conv", "vero_id",
        "oly_anon_id", "oly_enc_id", "rb_clickid", "sc_cid", "cmpid", "ncid", "ocid", "zanpid", "irclickid",
        "_kx", "trk_contact", "trk_msg", "trk_module", "trk_sid", "ref_src", "ref_url", "rdt", "guccounter",
        "guce_referrer", "guce_referrer_sig", "spm", "scm", "__s",
    };

    // Prefixes removed everywhere.
    private static readonly string[] GlobalPrefixes =
    {
        "utm_", "pk_", "mtm_", "hsa_", "pf_rd_", "pd_rd_", "oly_",
    };

    // Extra parameters per site. A key matches the host itself and any subdomain.
    private static readonly Dictionary<string, string[]> SiteParams = new(StringComparer.OrdinalIgnoreCase)
    {
        ["youtube.com"] = new[] { "si", "pp", "feature" },
        ["youtu.be"] = new[] { "si", "feature" },
        ["spotify.com"] = new[] { "si", "nd" },
        ["twitter.com"] = new[] { "s", "t" },
        ["x.com"] = new[] { "s", "t" },
        ["instagram.com"] = new[] { "img_index" },
        ["tiktok.com"] = new[] { "_r", "_t", "is_from_webapp", "sender_device", "share_app_id", "share_link_id",
            "social_sharing", "tt_from", "u_code", "is_copy_url", "checksum", "sec_user_id", "share_item_id",
            "timestamp", "user_id", "source", "web_id", "refer", "enter_from", "enter_method" },
        ["linkedin.com"] = new[] { "trk", "trkinfo", "trackingid", "refid", "lipi", "midtoken", "midsig", "eid", "otptoken" },
        ["reddit.com"] = new[] { "share_id", "ref", "ref_source", "rdt" },
        ["facebook.com"] = new[] { "mibextid", "ref", "__tn__", "__cft__[0]", "__xts__[0]", "sfnsn", "extid" },
        ["amazon.com"] = new[] { "ref", "ref_", "qid", "sr", "sprefix", "crid", "dib", "dib_tag", "content-id",
            "_encoding", "pd_rd_i", "pd_rd_r", "pd_rd_w", "pd_rd_wg", "linkcode", "tag", "linkid", "camp", "creative" },
        ["aliexpress.com"] = new[] { "algo_pvid", "algo_exp_id", "btsid", "ws_ab_test", "curpagelogUid", "utparam",
            "sk", "aff_fcid", "aff_fsk", "aff_platform", "aff_trace_key", "terminal_id", "afsmartredirect",
            "gatewayadapt", "_randl_currency", "_randl_shipto", "pdp_npi", "pdp_ext_f", "sourcetype" },
        ["aliexpress.ru"] = new[] { "algo_pvid", "algo_exp_id", "sk", "aff_fcid", "aff_fsk", "aff_platform",
            "aff_trace_key", "terminal_id", "gatewayadapt", "utparam", "_randl_currency", "_randl_shipto" },
        ["ebay.com"] = new[] { "_trkparms", "_trksid", "amdata", "hash", "mkcid", "mkrid", "mkevt", "campid", "customid", "toolid" },
        ["pinterest.com"] = new[] { "invite_code", "sender" },
    };

    // Google search: parameters worth keeping; everything else goes.
    private static readonly HashSet<string> GoogleSearchKeep = new(StringComparer.OrdinalIgnoreCase)
    {
        "q", "tbm", "tbs", "start", "udm", "hl", "gl", "safe", "num", "lr",
    };

    public static bool IsHttpUrl(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var t = text.Trim();
        if (t.Length > 8192 || t.Any(char.IsWhiteSpace)) return false;
        return Uri.TryCreate(t, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
               && !string.IsNullOrEmpty(uri.Host);
    }

    public static UrlCleanResult Clean(string input, UrlCleanerOptions? options = null)
    {
        options ??= new UrlCleanerOptions();
        var url = input.Trim();
        if (!IsHttpUrl(url)) return new UrlCleanResult(input, 0, false, Array.Empty<string>());

        bool unwrapped = false;
        if (options.UnwrapRedirects)
        {
            for (int depth = 0; depth < 3; depth++)
            {
                var inner = TryUnwrap(url);
                if (inner == null) break;
                url = inner;
                unwrapped = true;
            }
        }

        var uri = new Uri(url);
        var host = uri.Host;
        if (options.SkipDomains.Any(d => HostMatches(host, d.Trim())))
            return new UrlCleanResult(url, 0, unwrapped, Array.Empty<string>());

        var removed = new List<string>();
        var (beforeQuery, query, fragment) = Split(url);
        string? rebuiltQuery = query;

        if (query != null)
        {
            bool googleSearch = options.CleanSearchLinks && IsGoogleSearch(uri);
            var siteParams = SiteParamsFor(host);
            var extra = new HashSet<string>(options.ExtraParameters.Select(p => p.Trim()).Where(p => p.Length > 0),
                StringComparer.OrdinalIgnoreCase);

            var kept = new List<string>();
            foreach (var segment in query.Split('&'))
            {
                if (segment.Length == 0) continue;
                var name = DecodeName(segment);
                bool drop = GlobalParams.Contains(name)
                            || GlobalPrefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                            || siteParams.Contains(name)
                            || extra.Contains(name)
                            || (googleSearch && !GoogleSearchKeep.Contains(name));

                // YouTube "feature" is only tracking when it records a share.
                if (drop && name.Equals("feature", StringComparison.OrdinalIgnoreCase) && IsYouTube(host))
                {
                    var value = segment.Contains('=') ? segment[(segment.IndexOf('=') + 1)..] : "";
                    drop = value is "share" or "shared" or "youtu.be" or "emb_title" or "emb_logo" or "em-share_video_user";
                }

                if (drop) removed.Add(name);
                else kept.Add(segment);
            }
            rebuiltQuery = kept.Count > 0 ? string.Join("&", kept) : null;
        }

        // Amazon keeps a "/ref=…" tail in the path.
        if (SiteMatches(host, "amazon"))
        {
            int refIndex = beforeQuery.IndexOf("/ref=", StringComparison.OrdinalIgnoreCase);
            if (refIndex > 0)
            {
                beforeQuery = beforeQuery[..refIndex];
                removed.Add("ref");
            }
        }

        var sb = new StringBuilder(beforeQuery);
        if (rebuiltQuery != null) sb.Append('?').Append(rebuiltQuery);
        if (fragment != null) sb.Append('#').Append(fragment);
        var result = sb.ToString();
        return new UrlCleanResult(result, removed.Count, unwrapped, removed);
    }

    /// <summary>Returns the target of a known redirect wrapper, or null.</summary>
    public static string? TryUnwrap(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
        var host = uri.Host.ToLowerInvariant();
        var path = uri.AbsolutePath;
        string? param = null;

        if (IsGoogleHost(host) && path is "/url" or "/imgres") param = path == "/imgres" ? "imgurl" : "q|url";
        else if (host is "l.facebook.com" or "lm.facebook.com" or "l.messenger.com" && path == "/l.php") param = "u";
        else if (host == "l.instagram.com") param = "u";
        else if ((host is "vk.com" or "m.vk.com") && path == "/away.php") param = "to";
        else if (host == "away.vk.com") param = "to";
        else if (HostMatches(host, "youtube.com") && path == "/redirect") param = "q";
        else if (host == "out.reddit.com") param = "url";
        else if (host is "href.li") return Absolute(uri.Query.TrimStart('?'));
        else if (host == "t.umblr.com" && path == "/redirect") param = "z";
        else if (host is "slack-redir.net" && path == "/link") param = "url";

        if (param == null) return null;
        var query = uri.Query.TrimStart('?');
        foreach (var name in param.Split('|'))
        {
            foreach (var segment in query.Split('&'))
            {
                int eq = segment.IndexOf('=');
                if (eq <= 0) continue;
                if (!string.Equals(DecodeName(segment), name, StringComparison.OrdinalIgnoreCase)) continue;
                var value = Uri.UnescapeDataString(segment[(eq + 1)..].Replace('+', ' '));
                var target = Absolute(value);
                if (target != null) return target;
            }
        }
        return null;
    }

    private static string? Absolute(string value)
    {
        value = value.Trim();
        return IsHttpUrl(value) ? value : null;
    }

    private static (string BeforeQuery, string? Query, string? Fragment) Split(string url)
    {
        string? fragment = null;
        int hash = url.IndexOf('#');
        if (hash >= 0)
        {
            fragment = url[(hash + 1)..];
            url = url[..hash];
        }
        string? query = null;
        int q = url.IndexOf('?');
        if (q >= 0)
        {
            query = url[(q + 1)..];
            url = url[..q];
        }
        return (url, query, fragment);
    }

    private static string DecodeName(string segment)
    {
        int eq = segment.IndexOf('=');
        var raw = eq >= 0 ? segment[..eq] : segment;
        try { return Uri.UnescapeDataString(raw.Replace('+', ' ')); }
        catch (UriFormatException) { return raw; }
    }

    private static HashSet<string> SiteParamsFor(string host)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (site, names) in SiteParams)
        {
            if (HostMatches(host, site) || (site.StartsWith("amazon.", StringComparison.Ordinal) && SiteMatches(host, "amazon"))
                || (site.StartsWith("aliexpress.", StringComparison.Ordinal) && SiteMatches(host, "aliexpress")))
                set.UnionWith(names);
        }
        return set;
    }

    /// <summary>host equals domain or is a subdomain of it.</summary>
    public static bool HostMatches(string host, string domain)
    {
        if (string.IsNullOrEmpty(domain)) return false;
        domain = domain.TrimStart('.');
        return host.Equals(domain, StringComparison.OrdinalIgnoreCase)
               || host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>"www.amazon.co.uk" matches site "amazon".</summary>
    private static bool SiteMatches(string host, string site)
    {
        foreach (var label in host.Split('.'))
            if (label.Equals(site, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static bool IsGoogleHost(string host)
    {
        var labels = host.ToLowerInvariant().Split('.');
        // google.com, www.google.ru, google.co.uk …
        for (int i = 0; i < labels.Length; i++)
            if (labels[i] == "google" && i >= labels.Length - 3 && i < labels.Length - 1) return true;
        return false;
    }

    private static bool IsGoogleSearch(Uri uri) => IsGoogleHost(uri.Host) && uri.AbsolutePath == "/search";

    private static bool IsYouTube(string host) => HostMatches(host, "youtube.com") || HostMatches(host, "youtu.be");
}
