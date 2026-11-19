using System.ComponentModel.DataAnnotations;
public class Usuario
{
    [Key]
    public int Id {get;set;}
    public string User {get;set;}
    public string email {get;set;}
    public string Biografia {get;set;}
    public string Fecha {get;set;}
    public string Estado {get;set;}
    public string   GoogleId {get;set;}
    
}