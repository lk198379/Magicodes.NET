﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Magicodes.T4.Models;
using Magicodes.Web.Interfaces.T4;

//======================================================================
//
//        Copyright (C) 2014-2016 Magicodes团队    
//        All rights reserved
//
//        filename :T4Extensions
//        description :
//
//        created by 雪雁 at  2015/1/7 10:21:38
//        http://www.magicodes.net
//
//======================================================================
namespace Magicodes.T4.Extensions
{
    public static class T4Extensions
    {
        /// <summary>
        /// 获取T4生成特性
        /// </summary>
        /// <param name="pro"></param>
        /// <returns></returns>
        public static T4PropertyInfo GetT4PropertyInfo(this PropertyInfo pro)
        {
            var t4ProInfo = new T4PropertyInfo()
            {
                Name = pro.Name,
                Ignore = pro.GetAttribute<T4GenerationIgnoreAttribute>(false) != null,
                DataType = pro.GetT4DataType()
            };

            //显示名
            var displayAttribute = pro.GetAttribute<DisplayAttribute>(false);
            t4ProInfo.DisplayName = displayAttribute == null ? null : displayAttribute.Name;
            //是否必填
            var requiredAttribute = pro.GetAttribute<RequiredAttribute>(false);
            t4ProInfo.Required = requiredAttribute != null;

            //字符串长度
            var StringLengthAttribute = pro.GetAttribute<StringLengthAttribute>(false);
            t4ProInfo.MaxLength = StringLengthAttribute == null ? (int?)null : StringLengthAttribute.MaximumLength;

            //描述
            var descriptionAttribute = pro.GetAttribute<DescriptionAttribute>(false);
            t4ProInfo.Description = descriptionAttribute == null ? null : descriptionAttribute.Description;

            return t4ProInfo;
        }
        /// <summary>
        /// 获取程序集属性
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assembly"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static T GetAttribute<T>(this ICustomAttributeProvider assembly, bool inherit = false)
 where T : Attribute
        {

            return assembly
                .GetCustomAttributes(typeof(T), inherit)
                .OfType<T>()
                .FirstOrDefault();
        }
        /// <summary>
        /// 获取数据类型
        /// </summary>
        /// <param name="pro"></param>
        /// <returns></returns>
        public static T4DataType GetT4DataType(this PropertyInfo pro)
        {
            var proType = pro.PropertyType;
            //属性名
            var proName = pro.Name;
            //显示名
            var displayAttribute = GetAttribute<DisplayAttribute>(pro, false);
            var displayName = displayAttribute == null ? "" : displayAttribute.Name;
            //是否必填
            var requiredAttribute = GetAttribute<RequiredAttribute>(pro, false);
            var required = requiredAttribute != null;

            //字符串长度，大于50会生成TextArea，最大值默认4000
            var StringLengthAttribute = GetAttribute<StringLengthAttribute>(pro, false);
            var maxLength = StringLengthAttribute == null ? 4000 : StringLengthAttribute.MaximumLength;

            //是否邮箱地址
            var EmailAddressAttribute = GetAttribute<EmailAddressAttribute>(pro, false);
            var isEmail = EmailAddressAttribute != null;

            T4DataType? dataType = null;
            //数据类型
            var dt = GetAttribute<DataTypeAttribute>(pro, false);
            if (dt != null)
            {
                switch (dt.DataType)
                {
                    case DataType.CreditCard:
                        dataType = T4DataType.CreditCard;
                        break;
                    case DataType.Currency:
                        dataType = T4DataType.Currency;
                        break;
                    case DataType.Custom:
                        dataType = T4DataType.Custom;
                        break;
                    case DataType.Date:
                        dataType = T4DataType.Date;
                        break;
                    case DataType.DateTime:
                        dataType = T4DataType.DateTime;
                        break;
                    case DataType.Duration:
                        dataType = T4DataType.Duration;
                        break;
                    case DataType.EmailAddress:
                        dataType = T4DataType.EmailAddress;
                        break;
                    case DataType.Html:
                        dataType = T4DataType.Html;
                        break;
                    case DataType.ImageUrl:
                        dataType = T4DataType.ImageUrl;
                        break;
                    case DataType.MultilineText:
                        dataType = T4DataType.MultilineText;
                        break;
                    case DataType.Password:
                        dataType = T4DataType.Password;
                        break;
                    case DataType.PhoneNumber:
                        dataType = T4DataType.PhoneNumber;
                        break;
                    case DataType.PostalCode:
                        dataType = T4DataType.PostalCode;
                        break;
                    case DataType.Text:
                        dataType = T4DataType.Text;
                        break;
                    case DataType.Time:
                        dataType = T4DataType.Time;
                        break;
                    case DataType.Upload:
                        dataType = T4DataType.Upload;
                        break;
                    case DataType.Url:
                        dataType = T4DataType.Url;
                        break;
                    default:
                        break;
                }
            }
            if (isEmail) dataType = T4DataType.EmailAddress;

            if (dataType == null)
            {
                if (proType == typeof(String))
                {
                    if (maxLength <= 300)
                    {
                        dataType = T4DataType.Text;
                    }
                    else
                    {
                        dataType = T4DataType.MultilineText;
                    }
                }
                else if (proType == typeof(Boolean))
                    dataType = T4DataType.Bit;
                else if (proType == typeof(Int32) || proType == typeof(Int64))
                    dataType = T4DataType.Integer;
            }
            if (dataType == null)
                dataType = T4DataType.Text;
            return dataType.Value;
        }
        /// <summary>
        /// 获取数据类型
        /// </summary>
        /// <param name="pro"></param>
        /// <returns></returns>
        public static string T4Html(this PropertyInfo pro, Dictionary<T4DataType, string> dic, string defaultHtmlTemplate)
        {
            var t4ProInfo = pro.GetT4PropertyInfo();
            if (t4ProInfo.Ignore || t4ProInfo.Name.Equals("id", StringComparison.CurrentCultureIgnoreCase)) return null;
            var temp = string.Empty;
            if (dic.ContainsKey(t4ProInfo.DataType))
            {
                temp = dic[t4ProInfo.DataType];
            }
            else if (!string.IsNullOrWhiteSpace(defaultHtmlTemplate))
            {
                temp = defaultHtmlTemplate;
            }
            if (!string.IsNullOrEmpty(temp))
            {
                var str = temp;
                foreach (var proInfo in t4ProInfo.GetType().GetProperties())
                {
                    var name = "{" + proInfo.Name + "}";
                    var value = (proInfo.GetValue(t4ProInfo) ?? string.Empty).ToString();
                    if (proInfo.Name == "Required")
                        value = value.ToLower();
                    str = str.Replace(name, value);
                }
                return str;
            }
            return string.Empty;
        }
        public static string T4Html(this Type type, Dictionary<T4DataType, string> dic, string defaultHtmlTemplate)
        {
            if (type == null || dic == null) return string.Empty;
            var str = new StringBuilder();
            foreach (PropertyInfo pro in type.GetProperties())
            {
                var html = pro.T4Html(dic, defaultHtmlTemplate);
                if (!string.IsNullOrEmpty(html))
                    str.AppendLine(html);
            }
            return str.ToString();
        }
        /// <summary>
        /// 获取当前程序集中应用此特性的类
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static IEnumerable<Type> GetTypesWith<TAttribute>(this Assembly assembly, bool inherit) where TAttribute : System.Attribute
        {
            var attrType = typeof(TAttribute);
            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(attrType, true).Length > 0)
                {
                    yield return type;
                }
            }
            //return from t in assembly.GetTypes()
            //       where t.IsDefined(type, inherit)
            //       select t;
        }
    }
}