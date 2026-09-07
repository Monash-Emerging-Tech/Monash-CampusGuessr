using UnityEngine;

/// <summary>
/// Stores Supabase project settings for backend related scripts.
///
/// Written by jwon0200
/// Last Modified: 27/08/2026
/// </summary>
[CreateAssetMenu(menuName = "Supabase Config")]
public class SupabaseConfig : ScriptableObject
{
    [SerializeField] private string supabaseUrl;
    [SerializeField] private string publishableKey;
    // Edge Function region for Supabase is Sydney for now
    [SerializeField] private string functionRegion = "ap-southeast-2";

    public string SupabaseUrl => TrimTrailingSlash(supabaseUrl);
    public string PublishableKey => publishableKey?.Trim();
    public string FunctionRegion => functionRegion?.Trim();

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SupabaseUrl) &&
        !string.IsNullOrWhiteSpace(PublishableKey);

    public string GetFunctionUrl(string functionName)
    {
        string baseUrl = $"{SupabaseUrl}/functions/v1/{functionName}";

        if (string.IsNullOrWhiteSpace(FunctionRegion))
        {
            return baseUrl;
        }

        return $"{baseUrl}?forceFunctionRegion={FunctionRegion}";
    }

    private static string TrimTrailingSlash(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().TrimEnd('/');
    }
}
