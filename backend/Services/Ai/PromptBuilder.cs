using System.Text;
using LearnPath.API.DTOs.Ai;

namespace LearnPath.API.Services.Ai;

public class PromptBuilder
{
    private const string SystemInstruction =
        "You are an AI assignment feedback assistant for an LMS called LearnPath. " +
        "Your role is to help instructors by generating advisory feedback on student submissions. " +
        "You never assign the official grade. The instructor remains the final evaluator.";

    private const string OutputFormat =
        "Return your feedback in the following exact format using markdown headings. " +
        "Do not include any extra sections or commentary outside these headings:\n\n" +
        "## Summary\n[2-3 sentence summary of the student submission]\n\n" +
        "## Grammar & Language Feedback\n[Feedback on grammar, spelling, and language quality]\n\n" +
        "## Rubric Coverage\n[Which topics from the assignment rubric are covered and which are not. Use ✓ for covered and ✗ for not covered.]\n\n" +
        "## Missing Topics\n[List only the rubric topics that were not covered]\n\n" +
        "## Suggested Score\nPercentage: [0-100]\nMarks: [0-10]\n\n" +
        "## Overall Recommendation\n[Brief advisory recommendation to the instructor]";

    public string BuildPrompt(AiFeedbackRequestDto request)
    {
        var sb = new StringBuilder();
        sb.AppendLine(SystemInstruction);
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("Analyze the following student submission and generate feedback using the specified format.");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(request.AssignmentQuestion))
        {
            sb.AppendLine("## Assignment Question");
            sb.AppendLine(request.AssignmentQuestion);
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(request.AssignmentDescription))
        {
            sb.AppendLine("## Assignment Description");
            sb.AppendLine(request.AssignmentDescription);
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(request.AssignmentRubric))
        {
            sb.AppendLine("## Assignment Rubric");
            sb.AppendLine(request.AssignmentRubric);
            sb.AppendLine();
        }

        sb.AppendLine("## Student Submission");
        sb.AppendLine(request.StudentSubmission);
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(OutputFormat);

        return sb.ToString();
    }
}
