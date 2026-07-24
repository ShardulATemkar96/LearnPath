using LearnPath.API.DTOs.QuestionBank;

namespace LearnPath.API.Interfaces.Services;

public interface IValidationService
{
    List<ValidationError> Validate(string fileName, long fileSize, string jsonContent);
}
