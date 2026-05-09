using System.Windows;
using MahApps.Metro.Controls;
using SimplePhotoEditor.TemplateSelectors;
using Xunit;

namespace SimplePhotoEditor.Tests.TemplateSelectors
{
    public class MenuItemTemplateSelectorTests
    {
        [Fact]
        public void SelectTemplate_ReturnsGlyphTemplateForGlyphItem()
        {
            var glyphTemplate = new DataTemplate();
            var imageTemplate = new DataTemplate();
            var selector = new MenuItemTemplateSelector
            {
                GlyphDataTemplate = glyphTemplate,
                ImageDataTemplate = imageTemplate
            };

            var result = selector.SelectTemplate(new HamburgerMenuGlyphItem(), null);

            Assert.Same(glyphTemplate, result);
        }

        [Fact]
        public void SelectTemplate_ReturnsImageTemplateForImageItem()
        {
            var glyphTemplate = new DataTemplate();
            var imageTemplate = new DataTemplate();
            var selector = new MenuItemTemplateSelector
            {
                GlyphDataTemplate = glyphTemplate,
                ImageDataTemplate = imageTemplate
            };

            var result = selector.SelectTemplate(new HamburgerMenuImageItem(), null);

            Assert.Same(imageTemplate, result);
        }

        [Fact]
        public void SelectTemplate_ReturnsNullForUnknownItem()
        {
            var selector = new MenuItemTemplateSelector
            {
                GlyphDataTemplate = new DataTemplate(),
                ImageDataTemplate = new DataTemplate()
            };

            var result = selector.SelectTemplate(new object(), null);

            Assert.Null(result);
        }

        [Fact]
        public void SelectTemplate_ReturnsNullWhenItemIsNull()
        {
            var selector = new MenuItemTemplateSelector();

            var result = selector.SelectTemplate(null, null);

            Assert.Null(result);
        }
    }
}
