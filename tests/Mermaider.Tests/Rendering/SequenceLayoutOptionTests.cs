using System.Globalization;
using System.Xml.Linq;
using AwesomeAssertions;
using Mermaider;
using Mermaider.Models;

namespace Mermaider.Tests.Rendering;

public class SequenceLayoutOptionTests
{
	private const string Source = """
		sequenceDiagram
		Alice->>Bob: First
		Bob-->>Alice: Second
		""";

	[Test]
	public void Layer_spacing_controls_sequence_message_rows()
	{
		var defaultHeight = SvgHeight(MermaidRenderer.RenderSvg(Source));
		var spacedHeight = SvgHeight(MermaidRenderer.RenderSvg(Source, new RenderOptions { LayerSpacing = 100 }));

		spacedHeight.Should().BeGreaterThan(defaultHeight);
	}

	[Test]
	public void Sequence_message_margin_overrides_layer_spacing()
	{
		var layerHeight = SvgHeight(MermaidRenderer.RenderSvg(Source, new RenderOptions { LayerSpacing = 100 }));
		var messageHeight = SvgHeight(MermaidRenderer.RenderSvg(Source, new RenderOptions
		{
			LayerSpacing = 100,
			SequenceMessageMargin = 60,
		}));

		messageHeight.Should().BeLessThan(layerHeight);
	}

	[Test]
	public void Sequence_note_margin_changes_note_position()
	{
		const string source = """
			sequenceDiagram
			Alice->>Bob: First
			Note over Alice,Bob: Important
			""";

		var defaultNote = XDocument.Parse(MermaidRenderer.RenderSvg(source))
			.Descendants()
			.First(element => (string?)element.Attribute("class") == "note");
		var spacedNote = XDocument.Parse(MermaidRenderer.RenderSvg(source, new RenderOptions { SequenceNoteMargin = 30 }))
			.Descendants()
			.First(element => (string?)element.Attribute("class") == "note");

		double.Parse(spacedNote.Descendants().First(element => element.Name.LocalName == "rect").Attribute("y")!.Value, CultureInfo.InvariantCulture)
			.Should().BeGreaterThan(double.Parse(defaultNote.Descendants().First(element => element.Name.LocalName == "rect").Attribute("y")!.Value, CultureInfo.InvariantCulture));
	}

	[Test]
	public void Multiline_message_label_gets_additional_vertical_room()
	{
		var singleLine = SvgHeight(MermaidRenderer.RenderSvg("""
			sequenceDiagram
			Alice->>Bob: First
			Bob-->>Alice: Second
			"""));
		var multiline = SvgHeight(MermaidRenderer.RenderSvg("""
			sequenceDiagram
			Alice->>Bob: First<br/>continued<br/>again
			Bob-->>Alice: Second
			"""));

		multiline.Should().BeGreaterThan(singleLine);
	}

	[Test]
	public void Multiline_self_message_gets_additional_vertical_room()
	{
		var singleLine = SvgHeight(MermaidRenderer.RenderSvg("""
			sequenceDiagram
			Alice->>Alice: First
			Alice-->>Alice: Second
			"""));
		var multiline = SvgHeight(MermaidRenderer.RenderSvg("""
			sequenceDiagram
			Alice->>Alice: First<br/>continued
			Alice-->>Alice: Second
			"""));

		multiline.Should().BeGreaterThan(singleLine);
	}

	[Test]
	public void Multiline_message_label_stays_above_its_arrow()
	{
		var doc = XDocument.Parse(MermaidRenderer.RenderSvg("""
			sequenceDiagram
			Alice->>Bob: First<br/>continued<br/>again
			"""));
		var message = doc.Descendants().First(element => (string?)element.Attribute("class") == "message");
		var line = message.Descendants().First(element => element.Name.LocalName == "line");
		var text = message.Descendants().First(element => element.Name.LocalName == "text");
		var lineY = double.Parse(line.Attribute("y1")!.Value, CultureInfo.InvariantCulture);
		var finalBaseline = double.Parse(text.Attribute("y")!.Value, CultureInfo.InvariantCulture) +
			text.Elements().Sum(element => double.Parse(element.Attribute("dy")!.Value, CultureInfo.InvariantCulture));

		finalBaseline.Should().BeLessThan(lineY);
	}

	[Test]
	public void Multiline_message_space_is_allocated_before_the_message()
	{
		var doc = XDocument.Parse(MermaidRenderer.RenderSvg("""
			sequenceDiagram
			Alice->>Bob: First
			Bob-->>Alice: Second<br/>continued<br/>again
			Alice->>Bob: Third
			"""));
		var yPositions = doc.Descendants()
			.Where(element => (string?)element.Attribute("class") == "message")
			.Select(element => double.Parse(
				element.Descendants().First(child => child.Name.LocalName == "line").Attribute("y1")!.Value,
				CultureInfo.InvariantCulture))
			.ToArray();

		(yPositions[1] - yPositions[0]).Should().BeGreaterThan(yPositions[2] - yPositions[1]);
	}

	private static double SvgHeight(string svg) =>
		double.Parse(XDocument.Parse(svg).Root!.Attribute("height")!.Value, CultureInfo.InvariantCulture);
}
