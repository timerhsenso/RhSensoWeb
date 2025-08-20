using System.ComponentModel.DataAnnotations;

namespace RhSensoWeb.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "O campo Usuário é obrigatório.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo Senha é obrigatório.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Lembrar-me?")]
        public bool RememberMe { get; set; }
    }
}


