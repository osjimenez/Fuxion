# Semantic markup reference

Link to a topic and render the topic title as the text of the link:

<a href="About.topic"/>

Link to a topic and specify the text of the link:

<a href="About.topic">Link text</a>

Link to a specific anchor in the target topic:

[//]: # (<a href="About.topic" anchor="intro">Link to anchor</a>)

To add an external hyperlink, specify the full target URL in the href attribute:

<a href="https://www.jetbrains.com">Link to the JetBrains website</a>

# Paragraph

<p id="unique-id">
    Lorem ipsum dolor sit amet, consectetur
    adipiscing elit, sed do eiusmod tempor
    incididunt ut labore et dolore magna
    aliqua.
</p>
<p>
    Ut enim ad minim veniam, quis nostrud
    exercitation ullamco laboris nisi ut
    aliquip ex ea commodo consequat.
</p>

Use inline elements to indicate the semantic meaning of some text in paragraphs:.

<procedure id="example-procedure">
    <step>
        Click <control>OK</control>,
        call <code>doThis()</code>,
        open <path>file.txt</path>,
        select <ui-path>Edit | Copy</ui-path>,
        and stay <emphasis>focused</emphasis>.</step>
</procedure>

To format text directly with a specific color and font, use the 'format' element:

<format style="bold" color="#654321">Hello, world!</format>

<format style="subscript" color="Red">Hello, world!</format>

# Structural elements

## Chapters

<chapter title="Example chapter" id="example-chapter-id">
   <p>Some text.</p>
   <chapter title="Subchapter" id="subchapter">
       <p>Some more text.</p>
   </chapter>
</chapter>

## Procedures

<procedure title="Add something" id="procedure-id">
   <p>Before adding something, create it.</p>
   <step>Do this.</step>
   <step>Do that.</step>
   <p>Congratulation! You've added something.</p>
</procedure>

## Titles

<chapter title="Default chapter title" id="default-chapter-id">
    <title instance="foo">Chapter title for Foo</title>
    <p>Some text.</p>
</chapter>

# Collapsible elements

<chapter title="Some chapter" id="some_chapter" collapsible="true">
    <p>Some content.</p>
</chapter>

<procedure title="Some procedure" id="some_procedure" collapsible="true">
    <step>Do this</step>
    <step>Do that</step>
</procedure>

<code-block lang="kotlin" collapsible="true" collapsed-title="C# - Person.kt">
    class Person(val name: String) {
        val children: MutableList.Person. = mutableListOf()

        constructor(name: String, parent: Person) : this(name) {
            parent.children.add(this)
        }
    }
</code-block>

# Links and references
[GO](https://www.jetbrains.com/help/writerside/links-and-references.html#markdown)

<a href="About.topic">Link text</a>

<a href="About.topic" nullable="true">link text</a>

## See also

<seealso>
       <category ref="wrs">
           <a href="About.topic">Topic about links</a>
           <a href="About.topic"/>
       </category>
       <category ref="wrs">
           <a href="https://www.google.com">Google</a>
           <a href="https://www.jetbrains.com"/>
       </category>
</seealso>

# Images

<img src="logo.svg" alt="Alt text" width="450"/>

If the image and topic file are in the same directory:

[//]: # (<img src="./logo.svg" alt="Alt text" width="450"/>)

If the image is in some other location:

[//]: # (<img src="../myMediaDir/image.png" alt="Alt text" width="450"/>)

Or relative to the project root:

[//]: # (<img src="$PROJECT_DIR$/images/logo.svg" alt="Alt text" width="450"/>)

## Inline and block images

## Thumbnails

<img src="convert_table_to_xml.png" alt="Alt text" thumbnail="true"/>

# Videos

# List


<procedure id="lists-example">
    <step>
        <list type="decimal" start="2">
            <li>First item.
                <list type="alpha-lower">
                    <li>First indented item.</li>
                    <li>Second indented item.</li>
                </list>
            </li>
            <li>Second item.</li>
            <li>Third item.</li>
            <li>Fourth item.</li>
        </list>
    </step>
    <step>
        <list type="bullet">
            <li>Some list item</li>
            <li>Another list item</li>
            <li>Yet another list item
                <list type="bullet">
                    <li>Indented item</li>
                    <li>Indented item</li>
                </list>
            </li>
            <li>One more item</li>
        </list>
    </step>
</procedure>

## Multiple columns

<procedure>
<step>
<list columns="3">
    <li>this</li>
    <li>is</li>
    <li>a</li>
    <li>long</li>
    <li>list</li>
    <li>rendered</li>
    <li>in</li>
    <li>three</li>
    <li>columns</li>
</list>
</step>
</procedure>

## Definition lists

<deflist>
    <def title="First Term">
        This is the definition of the first term.
    </def>
    <def title="Second Term">
        This is the definition of the second term.
    </def>
</deflist>

# Tables

## Column width

<table>
    <tr>
        <td>Column</td>
        <td>Wide column</td>
        <td>Very wide column</td>
    </tr>
    <tr>
        <td>one</td>
        <td>two</td>
        <td>three</td>
    </tr>
    <tr>
        <td>one</td>
        <td>two</td>
        <td>three</td>
    </tr>
</table>

<table>
    <tr>
        <td width="300">Column</td>
        <td>Wide column</td>
        <td>Very wide column</td>
    </tr>
    <tr>
        <td>one</td>
        <td>two</td>
        <td>three</td>
    </tr>
    <tr>
        <td>one</td>
        <td>two</td>
        <td>three</td>
    </tr>
</table>

<table column-width="fixed">
    <tr>
        <td>Column</td>
        <td>Wide column</td>
        <td>Very wide column</td>
    </tr>
    <tr>
        <td>one</td>
        <td>two</td>
        <td>three</td>
    </tr>
    <tr>
        <td>one</td>
        <td>two</td>
        <td>three</td>
    </tr>
</table>

<table column-width="fixed">
    <tr>
        <td width="300">Column</td>
        <td>Wide column</td>
        <td>Very wide column</td>
    </tr>
    <tr>
        <td>one</td>
        <td>two</td>
        <td>three</td>
    </tr>
    <tr>
        <td>one</td>
        <td>two</td>
        <td>three</td>
    </tr>
</table>

# Tabs

<tabs>
    <tab id="windows-install" title="Windows">
        How to install on Windows.
    </tab>
    <tab id="macos-install" title="macOS">
        How to install on macOS.
    </tab>
    <tab id="linux-install" title="Linux">
        How to install on Linux.
    </tab>
</tabs>

## Topic-level switcher
<procedure>
<step>
<topic title="Topic title" switcher-label="Language">
    <chapter title="Examples" switcher-key="Java">
        <p>Some Java examples.</p>
    </chapter>
    <chapter title="Examples" switcher-key="Kotlin">
        <p>Some Kotlin examples.</p>
    </chapter>
</topic>
</step>
</procedure>

# Code

## Inline code

Call the <code>getAllItems()</code> method.

## Code blocks

<code-block lang="java">
    class MyClass {
        public static void main(String[] args) {
            System.out.println("Hello, World");
        }
    }
</code-block>

## XML and HTML code

<code-block lang="xml">
    <![CDATA[
        <some-tag>text in tag</some-tag>
    ]]>
</code-block>

## Links in code blocks

<code-block lang="java">
    class MyClass {
        public static void [[[main(String[] args)|https://en.wikipedia.org/wiki/Entry_point]]] {
            System.out.println("Hello, World");
        }
    }
</code-block>

## Reference code from file

<code-block lang="c#" src="sample1.cs"/>

[//]: # (<code-block lang="kotlin" src="../../../src/Fuxion/ConsoleTools.cs" region="ConsoleTools"/>)

## Compare code blocks

<compare>
    <code-block lang="kotlin">
        if (true) {
            doThis()
        }
    </code-block>
    <code-block lang="kotlin">
        if (true) doThis()
    </code-block>
</compare>

# Shortcuts

<p>Press <shortcut>Ctrl+C</shortcut> to copy.</p>

<p>Press <shortcut key="$Copy"/> to copy.</p>

# Tooltips

Send an <tooltip term="HTTP">HTTP</tooltip> request.

# Admonitions

## Tip

<tip>
    Use a tip to provide optional information or helpful advice,
    like an alternative way of doing something.
</tip>

## Note

<note>
    Use a note for important information that the reader should be aware of,
    like known issues or limitations.
</note>

## Warning

<warning>
    Use a warning for critical information about potentially harmful consequences,
    such as damage or data loss.
</warning>

# TLDR blocks

<tldr>
    <p>Shortcut: <shortcut>Ctrl+Space</shortcut></p>
    <p>Configure: <ui-path>Settings / Preferences | Editor | Code Completion</ui-path></p>
</tldr>

# Summary elements

<link-summary>Use link summaries to provide context for links.</link-summary>

<card-summary>Use card summaries to provide context for cards.</card-summary>

<link-summary rel="some-paragraph"/>

<p id="some-paragraph">Use this paragraph as a link summary</p>

## Card summary

<card-summary>Use card summaries to provide context for cards.</card-summary>

<code-block lang="mermaid">
sequenceDiagram
   Tech writer -->> Developer: Hi, can you check that I've described everything correctly?
   Developer -->> Junior developer: Hi, can you, please, help our TW with the task?
   Developer --x Tech writer: Sure, I've asked Garold to take care of this, it will help him to understand the logic better.
   Junior developer -->> Developer: No problem!

Developer --> Tech writer: Adding you both to a group chat  ...
Note right of Developer: Adding to the chat.

Tech writer --> Junior developer: Hi, Garold!
</code-block>

<code-block lang="mermaid">
mindmap
  root((mindmap))
    Origins
      Long history
      ::icon(fa fa-book)
      Popularisation
        British popular psychology author Tony Buzan
    Research
      On effectiveness<br/>and features
      On Automatic creation
        Uses
            Creative techniques
            Strategic planning
            Argument mapping
    Tools
      Pen and paper
      Mermaid
</code-block>


