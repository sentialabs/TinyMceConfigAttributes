using EPiServer.Core;

namespace TinyMceBlog.Models.Pages
{
    public interface IHasRelatedContent
    {
        ContentArea RelatedContentArea { get; }
    }
}
