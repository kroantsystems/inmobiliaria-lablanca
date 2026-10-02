using System.Reflection;
using LaBlanca.Domain.Common;
using LaBlanca.Domain.Leads;
using LaBlanca.Domain.Media;
using LaBlanca.Domain.Visits;
using LaBlanca.Shared.Localization;

namespace LaBlanca.UnitTests.Domain;

public class SlugGeneratorTests
{
    [Theory]
    [InlineData("Residencia Aura Light – Área 1", "residencia-aura-light-area-1")]
    [InlineData("  Casa   en Paraná Country Club!! ", "casa-en-parana-country-club")]
    [InlineData("Departamento Ñandutí 3° piso", "departamento-nanduti-3-piso")]
    [InlineData("Óga porã", "oga-pora")]
    public void Slugify_removes_accents_and_symbols(string title, string expected)
    {
        SlugGenerator.Slugify(title).Should().Be(expected);
    }

    [Fact]
    public void Slugify_limits_length_without_trailing_dash()
    {
        var slug = SlugGenerator.Slugify(new string('a', 70) + " " + new string('b', 30));

        slug.Length.Should().BeLessThanOrEqualTo(SlugGenerator.MaxLength);
        slug.Should().NotEndWith("-");
    }

    [Fact]
    public void MakeUnique_appends_numeric_suffix()
    {
        SlugGenerator.MakeUnique("casa", []).Should().Be("casa");
        SlugGenerator.MakeUnique("casa", ["casa"]).Should().Be("casa-2");
        SlugGenerator.MakeUnique("casa", ["casa", "casa-2", "casa-3"]).Should().Be("casa-4");
    }
}

public class MediaFileTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Documents_are_always_private()
    {
        var document = MediaFile.Create(null, "contrato.pdf", "k", "application/pdf", MediaKind.Document, 10, Guid.NewGuid(), Now, isPublic: true);

        document.IsPublic.Should().BeFalse();
        var act = () => document.SetPublic(true);
        act.Should().Throw<DomainException>().WithMessage(DomainErrors.DocumentsArePrivate);
    }

    [Fact]
    public void Only_images_can_be_cover()
    {
        var video = MediaFile.Create(Guid.NewGuid(), "tour.mp4", "k", "video/mp4", MediaKind.Video, 10, Guid.NewGuid(), Now, isPublic: true);

        var act = () => video.MarkAsCover();

        act.Should().Throw<DomainException>().WithMessage(DomainErrors.OnlyImagesCanBeCover);
    }
}

public class LeadAndVisitTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(LeadStatus.New, LeadStatus.VisitScheduled)]
    [InlineData(LeadStatus.Contacted, LeadStatus.VisitScheduled)]
    [InlineData(LeadStatus.Negotiating, LeadStatus.Negotiating)]
    [InlineData(LeadStatus.Won, LeadStatus.Won)]
    public void Scheduling_visit_advances_only_early_leads(LeadStatus initial, LeadStatus expected)
    {
        var lead = Lead.Create("Ana", "+595981000000", null, LeadInterest.BuyHouse, LeadSource.Manual, null, null, "es", null);
        lead.ChangeStatus(initial);

        lead.MarkVisitScheduled();

        lead.Status.Should().Be(expected);
    }

    [Fact]
    public void Visits_overlap_when_time_ranges_intersect()
    {
        var propertyId = Guid.NewGuid();
        var visit = Visit.Schedule(propertyId, null, "Ana", Now, 60, null);

        visit.Overlaps(Now.AddMinutes(59), 30).Should().BeTrue();
        visit.Overlaps(Now.AddMinutes(-30), 31).Should().BeTrue();
        visit.Overlaps(Now.AddMinutes(60), 30).Should().BeFalse();
        visit.Overlaps(Now.AddMinutes(-30), 30).Should().BeFalse();
    }

    [Fact]
    public void Cancelled_visit_never_overlaps()
    {
        var visit = Visit.Schedule(Guid.NewGuid(), null, "Ana", Now, 60, null);
        visit.Cancel();

        visit.Overlaps(Now, 60).Should().BeFalse();
        visit.Status.Should().Be(VisitStatus.Cancelled);
    }
}

public class DomainErrorsTests
{
    public static TheoryData<string> Keys()
    {
        var data = new TheoryData<string>();
        foreach (var field in typeof(DomainErrors).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            data.Add((string)field.GetValue(null)!);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Keys))]
    public void Every_domain_error_has_a_translated_message(string key)
    {
        AppMessages.ResourceManager.GetString(key, System.Globalization.CultureInfo.InvariantCulture).Should().NotBeNullOrEmpty();
    }
}
