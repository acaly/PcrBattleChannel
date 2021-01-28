using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace PcrBattleChannel.Models
{
    public static class ModelDisplayExtensions
    {
        public static IHtmlContent DisplayCharacterName<TModel>(this IHtmlHelper<TModel> html, string val)
        {
            var ret = new HtmlContentBuilder();
            var index = val.IndexOf('（');
            if (index == -1)
            {
                ret.AppendHtml(HttpUtility.HtmlEncode(val));
            }
            else
            {
                ret.AppendHtml(@"<span style=""display:inline-block"">");
                ret.AppendHtml(HttpUtility.HtmlEncode(val[0..index]));
                ret.AppendHtml("</span>");
                ret.AppendHtml(@"<span style=""display:inline-block"">");
                ret.AppendHtml(HttpUtility.HtmlEncode(val[index..]));
                ret.AppendHtml("</span>");
            }
            return ret;
        }

        public static string ToDamageString(this int damage)
        {
            if (damage > 1E9f)
            {
                return (damage / 1E9f).ToString("0.00") + " G";
            }
            else if (damage > 1E6f)
            {
                return (damage / 1E6f).ToString("0.00") + " M";
            }
            else if (damage > 1E3f)
            {
                return (damage / 1E3f).ToString("0.00") + " K";
            }
            return damage.ToString();
        }
    }
}
