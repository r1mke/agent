using AiAgents.BeeHiveAgent.Domain.Enums; // Treba ti za TaskType

namespace AiAgents.BeeHiveAgent.Web.DTOs;

public class UploadImageDto
{
    // Ovo je ključno: IFormFile unutar klase
    public IFormFile File { get; set; }

    // Ovdje možemo prebaciti i TaskType da sve ide kroz Formu
    public TaskType TaskType { get; set; } = TaskType.Pollen;
}