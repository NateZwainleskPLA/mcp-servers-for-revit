import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { getRevitConnectionDiagnostics } from "../utils/ConnectionManager.js";

export function registerGetRevitConnectionStatusTool(server: McpServer) {
  server.tool(
    "get_revit_connection_status",
    "Inspect the configured Revit MCP connection, including target version, port, plugin status, and legacy fallback warnings.",
    {},
    async () => {
      const diagnostics = await getRevitConnectionDiagnostics();

      return {
        content: [
          {
            type: "text",
            text: JSON.stringify(diagnostics, null, 2),
          },
        ],
      };
    }
  );
}
