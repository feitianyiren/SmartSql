﻿using SmartSql.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace SmartSql.Configuration.Tags.TagBuilders
{
    public abstract class AbstractTagBuilder : ITagBuilder
    {
        private const String PROPERTY_CHANGED = nameof(IPropertyChanged.PropertyChanged);
        private const String PREPEND = nameof(Tag.Prepend);
        private const String PROPERTY = nameof(Tag.Property);
        private const String REQUIRED = nameof(Tag.Required);
        private const String MIN = nameof(Dynamic.Min);
        private const String COMPARE_VALUE = nameof(NumericalCompareTag.CompareValue);

        public abstract ITag Build(XmlNode xmlNode, Statement statement);

        public String GetXmlAttributeValue(XmlNode xmlNode, string attributeName)
        {
            return xmlNode.Attributes?[attributeName]?.Value?.Trim();
        }

        public Decimal GetXmlAttributeValueAsDecimal(XmlNode xmlNode, string attributeName)
        {
            string strVal = GetXmlAttributeValue(xmlNode, attributeName);
            if (!Decimal.TryParse(strVal, out var decimalVal))
            {
                throw new SmartSqlException($"can not convert {strVal} to decimal from xml-node:{xmlNode.Value}.");
            }

            return decimalVal;
        }

        public bool TryGetXmlAttributeValueAsBool(XmlNode xmlNode, string attributeName, out bool boolVal)
        {
            string strVal = GetXmlAttributeValue(xmlNode, attributeName);
            if (String.IsNullOrEmpty(strVal))
            {
                boolVal = false;
                return false;
            }

            if (!Boolean.TryParse(strVal, out boolVal))
            {
                throw new SmartSqlException($"can not convert {strVal} to Boolean from xml-node:{xmlNode.Value}.");
            }

            return true;
        }

        public bool? GetPropertyChanged(XmlNode xmlNode)
        {
            if (TryGetXmlAttributeValueAsBool(xmlNode, PROPERTY_CHANGED, out var boolVal))
            {
                return boolVal;
            }
            return null;
        }

        public String GetPrepend(XmlNode xmlNode)
        {
            return GetXmlAttributeValue(xmlNode, PREPEND);
        }

        public String GetProperty(XmlNode xmlNode)
        {
            return GetXmlAttributeValue(xmlNode, PROPERTY);
        }

        public bool GetRequired(XmlNode xmlNode)
        {
            xmlNode.Attributes.TryGetValueAsBoolean(REQUIRED, out var requiredVal);
            return requiredVal;
        }

        public Int32? GetMin(XmlNode xmlNode)
        {
            if (xmlNode.Attributes.TryGetValueAsInt32(MIN, out var requiredVal))
            {
                return requiredVal;
            }

            return null;
        }

        public String GetCompareValue(XmlNode xmlNode)
        {
            return GetXmlAttributeValue(xmlNode, COMPARE_VALUE);
        }

        public Decimal GetCompareValueAsDecimal(XmlNode xmlNode)
        {
            return GetXmlAttributeValueAsDecimal(xmlNode, COMPARE_VALUE);
        }
    }
}