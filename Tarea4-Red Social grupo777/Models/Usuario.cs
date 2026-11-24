using System.ComponentModel.DataAnnotations;
public class Usuario
{
    [Key]
    public int Id {get;set;}
    public string User {get;set;} = string.Empty;
    public string email {get;set;} = string.Empty;
    public string Biografia {get;set;} = string.Empty;
    public string Fecha {get;set;} = string.Empty;
    public string Estado {get;set;} = string.Empty;
}