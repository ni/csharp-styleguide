<!-- This is to make the GitHub Page for the repo look nice  -->
<script src="https://cdn.jsdelivr.net/npm/anchor-js/anchor.min.js"></script>
<script>
document.addEventListener('DOMContentLoaded', function(event) {
  anchors.add("h1, h2, h3");
});
</script>

{% capture conventions %}{% include_relative Coding-Conventions.md %}{% endcapture %}
{% assign conventions2 = conventions | newline_to_br | split: "<br />" %}

{% assign auto_id = false %}
{% assign code_block = false %}

{% for line in conventions2 %}
    {%- if line contains "<!-- Begin Auto-ID -->" -%}
        {%- assign auto_id = true -%}
    {%- endif -%}

    {%- assign first_token = line | split: " " | first -%}
    {%- if first_token contains "```" -%}
        {%- if code_block -%}
            {%- assign code_block = false -%}
        {%- else -%}
            {%- assign code_block = true -%}
        {%- endif -%}
    {%- endif -%}

    {%- assign first_token_char = first_token | slice: 0 -%}
    {%- if auto_id and first_token_char == "#" and code_block == false -%}
        {% comment %}Omit hyphens, as we need the whitespace to denote a header{% endcomment %}
        {% assign header_text = line | split: first_token | last %}
        {% assign header_id = line | split: "]" | first | split: "[" | last | replace: ".","-"  %}
        {% if first_token != "###" %}
            {%- capture header_id -%}
                {%- comment -%}
                    I'm reasonably sure splitting by "]" is safe, as our
                    section/subsection headers are English words/phrases
                {%- endcomment -%}
                {{ header_id }}{{ header_text | split: "]" | last | replace: " ","-" | replace: "/","-" | downcase }}
            {%- endcapture %}
        {% endif %}

{{ first_token }} {{ header_text }} {#{{ header_id }}}
    {%- else -%}
{{ line }}
    {%- endif -%}

    {%- if line contains "<!-- TOC -->" -%}
- TOC
{:toc}
    {%- endif -%}
{%- endfor -%}
