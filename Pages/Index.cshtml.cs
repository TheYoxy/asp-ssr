using Microsoft.AspNetCore.Mvc.RazorPages;

namespace asp_ssr.Pages;

using Jering.Javascript.NodeJS;

public class Index : PageModel {
  private readonly INodeJSService _nodeJsService;
  private readonly ILogger<Index> _logger;

  public Index(INodeJSService nodeJsService, ILogger<Index> logger) {
    _nodeJsService = nodeJsService;
    _logger = logger;
  }

  public async Task OnGet(CancellationToken cancellationToken) {
    _logger.LogInformation("Reading index.html");
    var html = await System.IO.File.ReadAllTextAsync("ClientApp/dist/index.html", cancellationToken);

    _logger.LogInformation("Rendering client");
    var output = await _nodeJsService.InvokeFromStringAsync<string>(
      // language=JavaScript
      """
module.exports = (callback, url) => {
    function requireModule(modulePath, exportName) {
        try {
            const imported = require(modulePath);
            return exportName ? imported[exportName] : imported;
        } catch (err) {
            return err.code;
        }
    }

    const render = requireModule('./ClientApp/dist/server/entry-server.cjs', 'render');
    console.log('render', render);
    const context = {};
    const html = render(url, context);
    callback(null, html);
}
""", "render", null,
      new[] { "/", }, cancellationToken);
    
    var htmlContent = html.Replace("<!--app-html-->", output);
    _logger.LogInformation("Rendered output {Output}", htmlContent);
    
    ViewData["Html"] = htmlContent;
  }
}