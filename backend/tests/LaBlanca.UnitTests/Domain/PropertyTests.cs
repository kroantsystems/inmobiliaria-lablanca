using LaBlanca.Domain.Common;
using LaBlanca.Domain.Media;
using LaBlanca.Domain.Properties;

namespace LaBlanca.UnitTests.Domain;

public class PropertyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    private static Property NewProperty() => Property.Create(PropertyOperation.Sale, PropertyType.House, 350_000m, Currency.USD, Guid.NewGuid());

    private static MediaFile Image(Property property, bool isPublic = true) =>
        MediaFile.Create(property.Id, "foto.jpg", "2026/10/x.jpg", "image/jpeg", MediaKind.Image, 1024, Guid.NewGuid(), Now, isPublic);

    [Fact]
    public void New_property_is_unpublished_draft()
    {
        var property = NewProperty();

        property.Status.Should().Be(PropertyStatus.Draft);
        property.IsPublished.Should().BeFalse();
        property.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public void Publish_requires_available_status()
    {
        var property = NewProperty();
        property.AddMedia(Image(property));

        var act = () => property.Publish(Now);

        act.Should().Throw<DomainException>().WithMessage(DomainErrors.PropertyMustBeAvailableToPublish);
    }

    [Fact]
    public void Publish_requires_a_public_image()
    {
        var property = NewProperty();
        property.ChangeStatus(PropertyStatus.Available, Now);
        property.AddMedia(Image(property, isPublic: false));

        var act = () => property.Publish(Now);

        act.Should().Throw<DomainException>().WithMessage(DomainErrors.PropertyPublishRequiresImage);
    }

    [Fact]
    public void Publish_marks_published_with_date()
    {
        var property = NewProperty();
        property.ChangeStatus(PropertyStatus.Available, Now);
        property.AddMedia(Image(property));

        property.Publish(Now);

        property.IsPublished.Should().BeTrue();
        property.PublishedAt.Should().Be(Now);
    }

    [Fact]
    public void Only_published_property_can_be_featured()
    {
        var property = NewProperty();

        var act = () => property.SetFeatured(true);

        act.Should().Throw<DomainException>().WithMessage(DomainErrors.PropertyMustBePublishedToFeature);
    }

    [Fact]
    public void Sold_and_rented_record_completion_date()
    {
        var sold = NewProperty();
        sold.ChangeStatus(PropertyStatus.Sold, Now);
        sold.SoldAt.Should().Be(Now);

        var rented = Property.Create(PropertyOperation.Rent, PropertyType.Apartment, 2_200m, Currency.USD, Guid.NewGuid());
        rented.ChangeStatus(PropertyStatus.Rented, Now);
        rented.RentedAt.Should().Be(Now);
    }

    [Fact]
    public void Archive_unpublishes_and_removes_featured()
    {
        var property = NewProperty();
        property.ChangeStatus(PropertyStatus.Available, Now);
        property.AddMedia(Image(property));
        property.Publish(Now);
        property.SetFeatured(true);

        property.ChangeStatus(PropertyStatus.Archived, Now);

        property.IsPublished.Should().BeFalse();
        property.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public void Only_one_cover_per_property()
    {
        var property = NewProperty();
        var first = Image(property);
        var second = Image(property);
        property.AddMedia(first);
        property.AddMedia(second);

        property.SetCover(first.Id);
        property.SetCover(second.Id);

        first.IsCover.Should().BeFalse();
        second.IsCover.Should().BeTrue();
    }

    [Fact]
    public void Reorder_sets_sort_order_and_cover_comes_first_in_gallery()
    {
        var property = NewProperty();
        var a = Image(property);
        var b = Image(property);
        var c = Image(property);
        property.AddMedia(a);
        property.AddMedia(b);
        property.AddMedia(c);

        property.ReorderMedia([c.Id, a.Id, b.Id]);
        property.SetCover(b.Id);

        property.Gallery().Select(m => m.Id).Should().Equal(b.Id, c.Id, a.Id);
    }

    [Fact]
    public void Translation_is_replaced_per_locale()
    {
        var property = NewProperty();

        property.SetTranslation("es", "Casa", "casa", "Desc", null, null);
        property.SetTranslation("es", "Casa nueva", "casa-nueva", "Desc 2", "SEO", "SEO desc");

        property.Translations.Should().ContainSingle();
        property.TranslationFor("es")!.Title.Should().Be("Casa nueva");
        property.TranslationFor("pt").Should().BeNull();
    }
}
