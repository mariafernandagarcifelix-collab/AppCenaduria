using Supabase;
using AppCenaduria.Models;

namespace AppCenaduria.Controllers
{
    public class AltaMenuController
    {
        private readonly Supabase.Client _supabase;

        public AltaMenuController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<List<Platillo>> ObtenerPlatillosAsync()
        {
            var response = await _supabase.From<Platillo>().Get();
            return response.Models.OrderBy(p => p.Nombre).ToList();
        }

        public async Task<string> SubirFotoAsync(FileResult foto)
        {
            if (foto == null) return null;

            using var stream = await foto.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            string nombreArchivoUnico = $"{Guid.NewGuid()}_{foto.FileName}";
            await _supabase.Storage.From("platillos").Upload(bytes, nombreArchivoUnico);

            return _supabase.Storage.From("platillos").GetPublicUrl(nombreArchivoUnico);
        }

        public async Task GuardarPlatilloAsync(Platillo platillo)
        {
            await _supabase.From<Platillo>().Insert(platillo);
        }

        public async Task ActualizarPlatilloAsync(Platillo platillo)
        {
            await _supabase.From<Platillo>().Update(platillo);
        }

        public async Task EliminarPlatilloAsync(Platillo platillo)
        {
            await _supabase.From<Platillo>().Where(x => x.IdPlatillo == platillo.IdPlatillo).Delete();
        }
    }
}
