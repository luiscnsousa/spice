namespace Spice.TagHelpers
{
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.Routing;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    using Microsoft.AspNetCore.Razor.TagHelpers;
    using Spice.Models;

    [HtmlTargetElement("div", Attributes = "page-model")]
    public class PageLinkTagHelper : TagHelper
    {
        private IUrlHelperFactory urlHelperFactory;

        public PageLinkTagHelper(IUrlHelperFactory urlHelperFactory)
        {
            this.urlHelperFactory = urlHelperFactory;
        }

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }

        public PagingInfo PageModel { get; set; }

        public string PageAction { get; set; }

        public bool PageClassesEnabled { get; set; }

        public string PageClass { get; set; }

        public string PageClassNormal { get; set; }

        public string PageClassSelected { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var urlHelper = this.urlHelperFactory.GetUrlHelper(this.ViewContext);
            var result = new TagBuilder("div");

            for (var i = 1; i <= this.PageModel.TotalPages; i++)
            {
                var tag = new TagBuilder("a");
                var url = this.PageModel.UrlParam.Replace(":", i.ToString());
                tag.Attributes["href"] = url;

                if (this.PageClassesEnabled)
                {
                    tag.AddCssClass(this.PageClass);
                    tag.AddCssClass(
                        i == this.PageModel.CurrentPage
                            ? this.PageClassSelected
                            : this.PageClassNormal);
                }

                tag.InnerHtml.Append(i.ToString());
                result.InnerHtml.AppendHtml(tag);
            }

            output.Content.AppendHtml(result.InnerHtml);
        }
    }
}
