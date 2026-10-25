# Introduction

**Fooglud** is a client-side application designed to transform common data files, such as `.xlsx` and `.csv`, into database-ready data directly in the browser.

**Fooglud** is implemented in Blazor (C#), with AOT compilation and WebAssembly, to provide high performance and a smooth canvas experience while using fewer computational resources.

# Goals

In the AI era, filtering data in large spreadsheets, legacy systems, and old files has become increasingly important, especially for small companies. I created this application so anyone can transform their files into a relational database or a script for whatever they need to do:

No APIs, companies, or unnecessary services: just upload your file, select the options you need, draw your schema, choose the row types, and convert.

## Specific Objectives

- **Easy to understand:** A single page with three simple steps. You edit what you want, and Fooglud generates the output file for you.
- **Performance:** Process multiple files and large sheets quickly in your own browser. Fooglud does not support multithreading for strong parallelism yet, and low-allocation principles have not been fully established in practice.

# Why Parquet?

Parquet is currently used because the project is focused on SQL-related data. This may change in the future.

# Future

- CSV converter
- LanceDB and other similar export formats for agentic AI workflows
- Advanced script export options based on the selected types

# Dependencies

Special thanks to [`Sylvan.Data.Excel`](https://github.com/markpflug/Sylvan.Data.Excel).

**Note:** The application is still in an experimental phase and under active testing.
