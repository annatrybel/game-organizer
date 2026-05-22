using System.ComponentModel.DataAnnotations;

namespace GameOrganizer.Api.Models.Dto.Games
{
    public class GameDto
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Tytuł gry jest wymagany.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Tytuł musi mieć od 2 do 200 znaków.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Opis nie może przekraczać 2000 znaków.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Gatunek jest wymagany.")]
        [Range(1, int.MaxValue, ErrorMessage = "Wybrany gatunek jest nieprawidłowy.")]
        public int GenreId { get; set; }

        [Required(ErrorMessage = "Platforma jest wymagana.")]
        [Range(1, int.MaxValue, ErrorMessage = "Wybrana platforma jest nieprawidłowa.")]
        public int PlatformId { get; set; }
        public IFormFile? Image { get; set; }
    }
}
