using Portafolio.Models;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Portafolio.Servicios
{
    public interface IServiciosEmail
    {
        Task Enviar(ContactoViewModel contactoViewModel);
    }

    public class ServicioEmailSendGrid : IServiciosEmail
    {
        private readonly IConfiguration _configuration;

        public ServicioEmailSendGrid(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task Enviar(ContactoViewModel contactoViewModel)
        {
            var apiKey = _configuration.GetValue<string>("SENDGRID_API_KEY");
            var email = _configuration.GetValue<string>("SENDGRID_FROM");
            var nombre = _configuration.GetValue<string>("SENDGRID_NAME");

            var cliente = new SendGridClient(apiKey);
            var from = new EmailAddress(email,nombre);
            var subject = @$"El Cliente {contactoViewModel.Email} quiere contactarte";
            var to = new EmailAddress(email,nombre);
            var mensajeTextoPlano = contactoViewModel.Mensaje;
            var contenidoHtml = @$"De {contactoViewModel.Nombre} 
            Email : {contactoViewModel.Email}
            Mensaje : {contactoViewModel.Mensaje}";

            var singleEmail = MailHelper.CreateSingleEmail(from, to, subject, mensajeTextoPlano, contenidoHtml);
            var respuesta = await cliente.SendEmailAsync(singleEmail);

        }
    }
}
