using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Plugins.RZDAds.ApiSystem
{
    public static class WebUtils
    {
        public static string ToQueryUrl(string baseUrl, object queryObject)
        {
            if (queryObject == null)
                return baseUrl;

            var sb = new StringBuilder();
            sb.Append(baseUrl);
            sb.Append("?");

            var props = queryObject.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            bool first = true;
            foreach (var prop in props)
            {
                var value = prop.GetValue(queryObject);
                if (value == null) continue;

                if (!first) sb.Append("&");
                sb.Append($"{UnityWebRequest.EscapeURL(prop.Name)}={UnityWebRequest.EscapeURL(value.ToString())}");
                first = false;
            }

            return sb.ToString();
        }

        public static WWWForm TextureToForm(string textureFieldName, Texture2D texture)
        {
            var form = new WWWForm();
            form.AddBinaryData(textureFieldName, texture.EncodeToJPG(), ".jpg");
            return form;
        }

        public static WWWForm AddTextureToForm(this WWWForm form, string fieldName, Texture2D texture,
            int jpgQuality = 75)
        {
            byte[] imageData = texture.EncodeToJPG(jpgQuality);
            form.AddBinaryData(fieldName, imageData, "photo.jpg", "image/jpeg");
            return form;
        }

        public static string ConvertToArrayParams<T>(T dataClass)
        {
            var fieldsArray = dataClass.GetType().GetFields();
            var queryList = new List<string>();

            foreach (var field in fieldsArray)
            {
                var value = field.GetValue(dataClass);
                if (value == null)
                    continue;

                if (field.FieldType == typeof(string[]))
                {
                    var stringArray = (string[])value;
                    foreach (var item in stringArray)
                    {
                        if (!string.IsNullOrEmpty(item))
                        {
                            var query = $"{field.Name}={UnityWebRequest.EscapeURL(item)}";
                            queryList.Add(query);
                        }
                    }
                }
                else
                {
                    var query = $"{field.Name}={UnityWebRequest.EscapeURL(value.ToString())}";
                    queryList.Add(query);
                }
            }

            return queryList.Count > 0
                ? "?" + string.Join("&", queryList)
                : string.Empty;
        }

        // Ищет в адресе {fieldName} в шаблоне
        public static string ConvertToPathParams<T>(T dataClass, string templateUrl)
        {
            if (dataClass == null)
                throw new ArgumentNullException(nameof(dataClass));
            if (string.IsNullOrEmpty(templateUrl))
                throw new ArgumentNullException(nameof(templateUrl));

            var result = templateUrl;
            var fields = dataClass.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var value = field.GetValue(dataClass)?.ToString() ?? string.Empty;
                // заменяем {fieldName} на значение поля
                result = result.Replace("{" + field.Name + "}", value);
            }

            return result;
        }

        public static string ConvertToPathParams<T>(T dataClass)
        {
            var fieldsArray = dataClass.GetType().GetFields();
            var pathParams = new List<string>();

            foreach (var field in fieldsArray)
            {
                var value = field.GetValue(dataClass);
                if (value == null)
                    continue;
                pathParams.Add($"{value}");
            }

            return pathParams.Count > 0
                ? "/" + string.Join("/", pathParams)
                : string.Empty;
        }

        public static string CombineUrl(string baseUrl, string pathParam)
        {
            if (string.IsNullOrEmpty(baseUrl))
                return pathParam ?? "";

            if (string.IsNullOrEmpty(pathParam))
                return baseUrl;

            return $"{baseUrl.TrimEnd('/')}/{pathParam.TrimStart('/')}";
        }
    }
}