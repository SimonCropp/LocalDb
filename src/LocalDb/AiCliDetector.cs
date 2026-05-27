static class AiCliDetector
{
    static AiCliDetector()
    {
        var variables = Environment.GetEnvironmentVariables();

        // GitHub Copilot
        // https://docs.github.com/en/copilot/using-github-copilot/using-github-copilot-in-the-command-line
        var isCopilot = variables.Contains("GITHUB_COPILOT_RUNTIME");

        // Aider
        // https://aider.chat/docs/config/dotenv.html
        var isAider = variables.Contains("AIDER_GIT_DNAME") || variables.Contains("AIDER");

        // Claude Code
        // https://docs.anthropic.com/en/docs/build-with-claude/claude-cli
        var isClaudeCode = variables.Contains("CLAUDECODE") || variables.Contains("CLAUDE_CODE_ENTRYPOINT");

        // Cursor
        // https://cursor.com/docs/agent/terminal
        var isCursor = variables.Contains("CURSOR_AGENT");

        // Gemini CLI
        // https://google-gemini.github.io/gemini-cli/docs/tools/shell.html
        var isGeminiCli = variables.Contains("GEMINI_CLI");

        // OpenAI Codex CLI
        var isCodex = variables.Contains("CODEX_SANDBOX");

        // Amazon Q Developer CLI
        // https://docs.aws.amazon.com/amazonq/latest/qdeveloper-ug/command-line.html
        var isAmazonQ = variables.Contains("Q_TERM");

        // OpenCode
        var isOpenCode = variables.Contains("OPENCODE_CLIENT");

        // Cline
        var isCline = variables.Contains("CLINE_ACTIVE");

        // Augment Code
        var isAugment = variables.Contains("AUGMENT_AGENT");

        // TRAE AI
        var isTraeAi = variables.Contains("TRAE_AI_SHELL_ID");

        // Goose / Amp share the generic AGENT variable, distinguished by value
        var agent = Environment.GetEnvironmentVariable("AGENT");
        var isGoose = string.Equals(agent, "goose", StringComparison.OrdinalIgnoreCase);
        var isAmp = string.Equals(agent, "amp", StringComparison.OrdinalIgnoreCase);

        Detected = isCopilot ||
                   isAider ||
                   isClaudeCode ||
                   isCursor ||
                   isGeminiCli ||
                   isCodex ||
                   isAmazonQ ||
                   isOpenCode ||
                   isCline ||
                   isAugment ||
                   isTraeAi ||
                   isGoose ||
                   isAmp;
    }

    public static bool Detected { get; set; }

    public const string Prefix = "chatbot_";

    /// <summary>
    /// Prepends <see cref="Prefix"/> to <paramref name="name"/> when an AI CLI session is
    /// detected, isolating instance / storage names between AI and human-driven runs.
    /// Idempotent — a name that already starts with <see cref="Prefix"/> is returned unchanged.
    /// </summary>
    public static string PrefixIfDetected(string name)
    {
        if (!Detected || name.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return name;
        }

        return Prefix + name;
    }

    /// <summary>
    /// Prepends <see cref="Prefix"/> to the leaf segment of <paramref name="directory"/> when
    /// an AI CLI session is detected, so AI and human runs don't share an on-disk template
    /// folder even when callers supply an explicit directory.
    /// Idempotent — a leaf that already starts with <see cref="Prefix"/> is returned unchanged.
    /// </summary>
    public static string PrefixDirectoryIfDetected(string directory)
    {
        if (!Detected)
        {
            return directory;
        }

        var leaf = Path.GetFileName(directory);
        if (leaf.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return directory;
        }

        var parent = Path.GetDirectoryName(directory);
        var prefixedLeaf = Prefix + leaf;
        return parent is null ? prefixedLeaf : Path.Combine(parent, prefixedLeaf);
    }
}
