﻿using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Drawing;
using QuestPDF.Elements;
using QuestPDF.Elements.Text;
using QuestPDF.Elements.Text.Items;
using QuestPDF.Infrastructure;
using static System.String;

namespace QuestPDF.Fluent
{
    public class TextSpanDescriptor
    {
        internal TextStyle TextStyle { get; }

        internal TextSpanDescriptor(TextStyle textStyle)
        {
            TextStyle = textStyle;
        }
    }

    public delegate string PageNumberFormatter(int? pageNumber);
    
    public class TextPageNumberDescriptor : TextSpanDescriptor
    {
        internal PageNumberFormatter FormatFunction { get; private set; } = x => x?.ToString() ?? string.Empty;

        internal TextPageNumberDescriptor(TextStyle textStyle) : base(textStyle)
        {
            
        }

        public TextPageNumberDescriptor Format(PageNumberFormatter formatter)
        {
            FormatFunction = formatter ?? FormatFunction;
            return this;
        }
    }
    
    public class TextDescriptor
    {
        internal TextBlock TextBlock = new();
        private TextStyle DefaultStyle { get; set; } = TextStyle.Default;
        internal HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

        public void DefaultTextStyle(TextStyle style)
        {
            DefaultStyle = style;
        }
        
        public void DefaultTextStyle(Func<TextStyle, TextStyle> style)
        {
            DefaultStyle = style(TextStyle.Default);
        }
        
        public void AlignLeft()
        {
            Alignment = HorizontalAlignment.Left;
        }
        
        public void AlignCenter()
        {
            Alignment = HorizontalAlignment.Center;
        }
        
        public void AlignRight()
        {
            Alignment = HorizontalAlignment.Right;
        }

        public void ParagraphSpacing(float value, Unit unit = Unit.Point)
        {
            TextBlock.ParagraphSpacing = value.ToPoints(unit);
        }

        [Obsolete("This element has been renamed since version 2022.3. Please use the overload that returns a TextSpanDescriptor object which allows to specify text style.")]
        public void Span(string? text, TextStyle style)
        {
            Span(text).Style(style);
        }
        
        public TextSpanDescriptor Span(string? text)
        {
            var style = DefaultStyle.Clone();
            var descriptor = new TextSpanDescriptor(style);

            if (text == null)
                return descriptor;
            
            TextBlock.Items.Add(new TextBlockSpan
            {
                Text = text,
                Style = style
            });
            
            return descriptor;
        }

        public TextSpanDescriptor Line(string? text)
        {
            text ??= string.Empty;
            return Span(text + Environment.NewLine);
        }

        public TextSpanDescriptor EmptyLine()
        {
            return Span(Environment.NewLine);
        }
        
        private TextPageNumberDescriptor PageNumber(Func<IPageContext, int?> pageNumber)
        {
            var style = DefaultStyle.Clone();
            var descriptor = new TextPageNumberDescriptor(style);
            
            TextBlock.Items.Add(new TextBlockPageNumber
            {
                Source = context => descriptor.FormatFunction(pageNumber(context)),
                Style = style
            });
            
            return descriptor;
        }

        public TextPageNumberDescriptor CurrentPageNumber()
        {
            return PageNumber(x => x.CurrentPage);
        }
        
        public TextPageNumberDescriptor TotalPages()
        {
            return PageNumber(x => x.GetLocation(PageContext.DocumentLocation)?.Length);
        }

        [Obsolete("This element has been renamed since version 2022.3. Please use the BeginPageNumberOfSection method.")]
        public void PageNumberOfLocation(string locationName, TextStyle? style = null)
        {
            BeginPageNumberOfSection(locationName).Style(style);
        }
        
        public TextPageNumberDescriptor BeginPageNumberOfSection(string locationName)
        {
            return PageNumber(x => x.GetLocation(locationName)?.PageStart);
        }
        
        public TextPageNumberDescriptor EndPageNumberOfSection(string locationName)
        {
            return PageNumber(x => x.GetLocation(locationName)?.PageEnd);
        }
        
        public TextPageNumberDescriptor PageNumberWithinSection(string locationName)
        {
            return PageNumber(x => x.CurrentPage + 1 - x.GetLocation(locationName)?.PageStart);
        }
        
        public TextPageNumberDescriptor TotalPagesWithinSection(string locationName)
        {
            return PageNumber(x => x.GetLocation(locationName)?.Length);
        }
        
        public TextSpanDescriptor SectionLink(string? text, string sectionName)
        {
            if (IsNullOrEmpty(sectionName))
                throw new ArgumentException("Section name cannot be null or empty", nameof(sectionName));

            var style = DefaultStyle.Clone();
            var descriptor = new TextSpanDescriptor(style);
            
            if (IsNullOrEmpty(text))
                return descriptor;
            
            TextBlock.Items.Add(new TextBlockSectionLink
            {
                Style = style,
                Text = text,
                SectionName = sectionName
            });

            return descriptor;
        }
        
        [Obsolete("This element has been renamed since version 2022.3. Please use the SectionLink method.")]
        public void InternalLocation(string? text, string locationName, TextStyle? style = null)
        {
            SectionLink(text, locationName).Style(style);
        }
        
        public TextSpanDescriptor Hyperlink(string? text, string url)
        {
            if (IsNullOrEmpty(url))
                throw new ArgumentException("Url cannot be null or empty", nameof(url));

            var style = DefaultStyle.Clone();
            var descriptor = new TextSpanDescriptor(style);

            if (IsNullOrEmpty(text))
                return descriptor;
            
            TextBlock.Items.Add(new TextBlockHyperlink
            {
                Style = style,
                Text = text,
                Url = url
            });

            return descriptor;
        }
        
        [Obsolete("This element has been renamed since version 2022.3. Please use the Hyperlink method.")]
        public void ExternalLocation(string? text, string url, TextStyle? style = null)
        {
            Hyperlink(text, url).Style(style);
        }
        
        public IContainer Element()
        {
            var container = new Container();
                
            TextBlock.Items.Add(new TextBlockElement
            {
                Element = container
            });
            
            return container.AlignBottom().MinimalBox();
        }
    }
    
    public static class TextExtensions
    {
        public static void Text(this IContainer element, Action<TextDescriptor> content)
        {
            var descriptor = new TextDescriptor();
            
            if (element is Alignment alignment)
                descriptor.Alignment = alignment.Horizontal;
            
            content?.Invoke(descriptor);
            element.Element(descriptor.TextBlock);
        }
        
        [Obsolete("This element has been renamed since version 2022.3. Please use the overload that returns a TextSpanDescriptor object which allows to specify text style.")]
        public static void Text(this IContainer element, object? text, TextStyle style)
        {
            element.Text(text).Style(style);
        }
        
        public static TextSpanDescriptor Text(this IContainer element, object? text)
        {
            var descriptor = (TextSpanDescriptor) null;
            element.Text(x => descriptor = x.Span(text?.ToString()));
            return descriptor;
        }
    }
}