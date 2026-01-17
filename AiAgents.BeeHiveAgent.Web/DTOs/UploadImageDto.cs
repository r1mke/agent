using AiAgents.BeeHiveAgent.Domain.Enums;
namespace AiAgents.BeeHiveAgent.Web.DTOs;

public class UploadImageDto
{

    public IFormFile File { get; set; }


    public TaskType TaskType { get; set; } = TaskType.Pollen;
}