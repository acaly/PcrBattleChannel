using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace PcrBattleChannel.Pages
{
    public static class PageHelper
    {
        public static IHtmlContent HelpMessage<TModel>(this IHtmlHelper<TModel> helper, string msg, string title = "帮助")
        {
            var ret = new HtmlContentBuilder();
            ret.AppendHtml(@$"<a href=""javascript:void(0)"" title=""{title}"" data-toggle=""popover"" data-trigger=""focus"" data-content=""");
            ret.AppendHtml(HttpUtility.HtmlEncode(msg));
            ret.AppendHtml(@""" data-placement=""top"">(?)</a>");
            return ret;
        }

        public static IHtmlContent Guessed<TModel>(this IHtmlHelper<TModel> helper)
        {
            var ret = new HtmlContentBuilder();
            ret.AppendHtml("（推测）");
            ret.AppendHtml(helper.HelpMessage("根据yobot数据和已预约的套餐推测的结果。请在下方确认或修正。"));
            return ret;
        }
    }
}
