using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        protected virtual async Task<Response> ExecuteSendEmailAsync(SendGridMessage message)
        {

            var sendGridClient = new SendGridClient("ApiKey");
            var response = await sendGridClient.SendEmailAsync(message).ConfigureAwait(false);
            return response;
        }

        public override bool SendEmail(string subject, string body, List<string> toList, string from,
            List<string> bccList, bool isBodyHtml = false,
            List<AttachmentInfo> attachments = null)

        {

            try
            {
                if (toList == null || toList.Count == 0)
                {
                    return false;
                }

                var message = new SendGridMessage

                {
                    From = new EmailAddress(from),
                    Subject = subject
                };


                if (isBodyHtml)
                {
                    message.HtmlContent = body;
                }

                else
                {
                    message.PlainTextContent = body;
                }


                message.AddTos(toList.Select(toAddress => new EmailAddress(toAddress)).ToList());

                if (bccList != null && bccList.Any())

                {

                    var filteredBccEmails =
                        EmailUtils.FilterBccEmailsFromToEmails(toList, bccList);

                    if (filteredBccEmails.Any())
                    {
                        message.AddBccs(filteredBccEmails.Select(bccAddress => new EmailAddress(bccAddress)).ToList());
                    }

                }


                if (attachments != null && attachments.Any())
                {
                    var sendGridAttachments = new List<Attachment>();

                    if (attachments.Sum(s => s.Stream.Length) >= AttachmentLimits.AttachmentMaxSizeLimit)
                    {
                        throw new AttachmentLimitExceededException(
                            AttachmentLimits.AttachmentLimitExceededExceptionMessage);
                    }

                    foreach (var attachment in attachments)
                    {
                        var bData = new byte[attachment.Stream.Length];
                        attachment.Stream.Read(bData, 0, Convert.ToInt32(attachment.Stream.Length));
                        var content = Convert.ToBase64String(bData);
                        sendGridAttachments.Add(new Attachment {Content = content, Filename = attachment.Name});
                    }

                    message.AddAttachments(sendGridAttachments);

                }

                var response = ExecuteSendEmailAsync(message).ConfigureAwait(false).GetAwaiter().GetResult();

                if (response.StatusCode == HttpStatusCode.Accepted)
                {
                    return true;
                }

                throw new Exception(

                    $"Email not delivered via SendGridAPI with response status: {response.StatusCode} {Environment.NewLine} response content: {response.Body.ReadAsStringAsync().Result}");

            }
            catch (Exception ex)
            {
                throw new EmailException(ex.Message, subject, body, from, toList, bccList, ex);
            }
        }
    }
}
