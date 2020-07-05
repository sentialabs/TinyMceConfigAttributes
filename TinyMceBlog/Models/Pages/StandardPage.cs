using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using TinyMceBlog.Business.Attributes;

namespace TinyMceBlog.Models.Pages
{
    /// <summary>
    /// Used for the pages mainly consisting of manually created content such as text, images, and blocks
    /// </summary>
    [SiteContentType(GUID = "9CCC8A41-5C8C-4BE0-8E73-520FF3DE8267")]
    [SiteImageUrl(Global.StaticGraphicsFolderPath + "page-type-thumbnail-standard.png")]
    public class StandardPage : SitePageData
    {
        [Display(
            GroupName = SystemTabNames.Content,
            Order = 310)]
        [CultureSpecific]
        [SimpleTinyMce]
        public virtual XhtmlString MainBody { get; set; }

        [Display(
            GroupName = SystemTabNames.Content,
            Order = 320)]
        [SimpleTinyMce]
        public virtual ContentArea MainContentArea { get; set; }
    }
}
