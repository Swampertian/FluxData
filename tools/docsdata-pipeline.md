# DocsData: fluxo completo

Este diagrama descreve a execucao atual do sistema Blazor WebAssembly, desde a
selecao do arquivo Excel ate o download do ZIP com os arquivos Parquet.

```mermaid
flowchart TD
    Start([Aplicacao inicia]) --> Program[Program.cs\nWebAssemblyHostBuilder\nregistra dependencias e inicia RunAsync]
    Program --> Home[Home.razor + Home.razor.cs\nrota /]

    subgraph Browser[Browser e UI Blazor]
        Input[InputFile\nDropZone.razor]
        Select[DropZone.OnFileSelected\nIBrowserFile]
        FileInfo[Tela de arquivo selecionado\nProcessar planilhas]
        Edit[SheetCard dentro de um node do canvas\ninclude/rename/type]
        Actions[ActionBar flutuante\nAplicar alteracoes / Exportar Parquet]
        Reset[Novo arquivo\nHome.Reset]
    end

    subgraph Pipeline[ExcelPipeline]
        Upload[ExcelPipeline.UploadAsync]
        Process[ExcelPipeline.ProcessAsync]
        Transform[ExcelPipeline.TransformAsync]
        Export[ExcelPipeline.ExportAsync]
        StageVal[RunStage\nValidation]
        StageIngest[RunStageAsync\nIngestion]
        StageParse[RunStage\nParsing]
        StageTransform[RunStage\nTransform]
        StageExport[RunStageAsync\nExport]
    end

    subgraph Domain[Etapas de dominio]
        Validator[ExcelFileValidator.IsValid\n.xlsx / .xlsm / .xls]
        Ingestor[BrowserFileIngestor.ReadAsync\nmaximo 25 MB\nIBrowserFile -> byte[]]
        Parser[ExcelWorkbookParser.Parse\nSylvan.Data.Excel]
        ReadSheet[ParseSheet / ReadCell\nleitura forward-only\nWorkbookData]
        Mapper[SheetEditMapper.BuildSheetEdits]
        Selection[SheetSelectionValidator.Validate]
        Request[SheetEditMapper.BuildTransformRequest]
        Transformer[WorkbookTransformer.Transform]
        SelectColumns[SelectedColumns\nfiltra planilhas e colunas]
        Convert[CellTypeConverter.Convert\nstring/double/datetime/bool]
        Exporter[ParquetWorkbookExporter.ExportAsync]
        Zip[ZipArchive\n1 .parquet por planilha\nbyte[]]
    end

    subgraph Canvas[Schema canvas interativo]
        Rebuild[Home.RebuildDiagramNodes\num SheetNode por SheetEditState]
        BlazorDiagram[BlazorDiagram + DiagramCanvas\nZ.Blazor.Diagrams, C#/Blazor puro]
        NodeWidget[SheetNodeWidget\nrenderiza o SheetCard real dentro do node]
    end

    subgraph JS[Interoperabilidade e download]
        DownloadCall[Home.ExportAsync\nIJSRuntime.InvokeVoidAsync]
        Download[downloadFileFromBytes\nUint8Array -> Blob -> link.click]
    end

    Home --> Input
    Input --> Select
    Select -->|OnFile| HomeUpload[Home.OnFileSelected]
    HomeUpload --> Upload
    Upload --> StageVal
    StageVal --> Validator
    Validator -->|valido| StageIngest
    Validator -->|invalido| Error[ExcelConverterException\nmensagem segura na UI]
    StageIngest --> Ingestor
    Ingestor --> UploadResult[UploadResult\nFileName + Content]
    UploadResult --> FileInfo

    FileInfo -->|ProcessAsync| HomeProcess[Home.ProcessAsync]
    HomeProcess --> Process
    Process --> StageParse
    StageParse --> Parser
    Parser --> ReadSheet
    ReadSheet --> Workbook[WorkbookData\nlista de SheetData]
    Workbook --> Mapper
    Mapper --> Rebuild
    Rebuild --> BlazorDiagram
    BlazorDiagram --> NodeWidget
    NodeWidget --> Edit

    Edit -->|mudanca de coluna/planilha| NodeEdit[SheetNodeWidget muta\na mesma SheetEditState do node]
    NodeEdit -->|Node.OnChanged| StateHasChanged[Home.StateHasChanged\natualiza a toolbar flutuante]

    Edit --> Actions
    Actions -->|ApplyTransformAsync| Apply[Home.ApplyTransformAsync]
    Apply --> Selection
    Selection -->|erro| Error
    Selection -->|valido| Request
    Request --> Transform
    Transform --> StageTransform
    StageTransform --> Transformer
    Transformer --> SelectColumns
    SelectColumns --> Convert
    Convert --> Transformed[WorkbookData transformado]
    Transformed --> Mapper
    Mapper --> Rebuild

    Actions -->|ExportAsync| ExportCheck{hasPendingSelection?}
    ExportCheck -->|sim| ErrorPending[Mensagem: aplique as alteracoes]
    ExportCheck -->|nao| HomeExport[Home.ExportAsync]
    HomeExport --> Export
    Export --> StageExport
    StageExport --> Exporter
    Exporter --> Zip
    Zip --> DownloadCall
    DownloadCall --> Download
    Download --> File[Download do .zip pelo navegador]

    Reset -->|Home.Reset| ClearNodes[Diagram.Nodes.Clear]
    Reset --> Input

    StageVal -. excecao .-> Error
    StageIngest -. excecao .-> Error
    StageParse -. excecao .-> Error
    StageTransform -. excecao .-> Error
    StageExport -. excecao .-> Error
```

## Mapa de metodos

| Momento | Metodo | Responsabilidade | Resultado |
| --- | --- | --- | --- |
| Inicializacao | `Program.cs` (top-level) | Cria o host WASM e registra as implementacoes das interfaces | Aplicacao pronta no browser |
| Selecao | `DropZone.OnFileSelected` | Recebe o `IBrowserFile` do `InputFile` | Chama `Home.OnFileSelected` |
| Upload local | `ExcelPipeline.UploadAsync` | Orquestra validacao e ingestao | `UploadResult` |
| Validacao | `ExcelFileValidator.IsValid` | Valida a extensao do nome | `bool` |
| Ingestao | `BrowserFileIngestor.ReadAsync` | Le o stream do browser ate 25 MB | `byte[]` |
| Processamento | `ExcelPipeline.ProcessAsync` | Executa a etapa de parsing em `Task.Run` | `WorkbookData` |
| Parsing | `ExcelWorkbookParser.Parse` | Abre o Excel e percorre todas as planilhas | `WorkbookData` com valores CLR |
| Preparacao da UI | `SheetEditMapper.BuildSheetEdits` | Converte dominio em estado editavel | `List<SheetEditState>` |
| Montagem do canvas | `Home.RebuildDiagramNodes` | Cria um `SheetNode` (layout em grade) por `SheetEditState` | `BlazorDiagram.Nodes` populado |
| Edicao no node | `SheetNodeWidget` (`OnIncludedChanged` / `OnTargetNameChanged` / `OnColumnChanged`) | Muta o `SheetEditState` do node diretamente; sem JSInterop, sem modelo de schema separado | `Node.OnChanged` dispara `Home.StateHasChanged` |
| Validacao da selecao | `SheetSelectionValidator.Validate` | Verifica planilha, colunas, nomes e duplicidades | `string?` |
| Montagem da transformacao | `SheetEditMapper.BuildTransformRequest` | Converte estado da UI em request de dominio | `WorkbookTransformRequest` |
| Transformacao | `WorkbookTransformer.Transform` | Filtra, renomeia e altera tipos | `WorkbookData` transformado |
| Conversao de celula | `CellTypeConverter.Convert` | Converte valores para o tipo escolhido | valor CLR ou `null` |
| Exportacao | `ExcelPipeline.ExportAsync` | Executa a etapa de export em `Task.Run` | `byte[]` do ZIP |
| Geracao Parquet | `ParquetWorkbookExporter.ExportAsync` | Cria um arquivo Parquet por planilha | ZIP em memoria |
| Download | `downloadFileFromBytes` | Recebe `Uint8Array`, cria `Blob` e aciona `link.click()` | Arquivo baixado |
| Limpeza | `Home.Reset` | `Diagram.Nodes.Clear()` | Canvas esvaziado, volta ao DropZone |

## Pontos de memoria e JSInterop

- O arquivo de entrada permanece em memoria como `UploadResult.Content` (`byte[]`).
- O parser materializa o workbook como `WorkbookData`, incluindo todas as linhas.
- O exportador materializa o ZIP inteiro como `byte[]` antes do download.
- `Home.ExportAsync` passa o `byte[]` direto ao JS: o interop do Blazor transfere arrays de bytes
  como `Uint8Array` binario, sem Base64.
- `downloadFileFromBytes` cria o `Blob` direto do `Uint8Array` recebido.
- Nao existe API/backend neste projeto: o processamento e a exportacao ocorrem no WASM.
- `IWorkbookExporter` participa somente da exportacao; ele nao carrega o arquivo de entrada.

## Tratamento de erros

`ExcelPipeline.RunStage` e `RunStageAsync` registram a etapa e convertem excecoes
inesperadas em `ExcelConverterException` com mensagem segura. `Home.razor.cs`
captura essa excecao e atualiza `errorMessage` para exibicao na UI.

## Registro das implementacoes

As dependencias sao registradas em `Program.cs`:

- `IFileIngestor` -> `BrowserFileIngestor`
- `IWorkbookParser` -> `ExcelWorkbookParser`
- `IWorkbookTransformer` -> `WorkbookTransformer`
- `IWorkbookExporter` -> `ParquetWorkbookExporter`
- `IExcelPipeline` -> `ExcelPipeline`

O canvas (`BlazorDiagram`) nao passa por DI: e uma instancia por pagina (`Home.Diagram`),
criada e registrada (`Diagram.RegisterComponent<SheetNode, SheetNodeWidget>()`) em
`Home.OnInitialized`.
