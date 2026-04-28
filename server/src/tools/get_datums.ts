import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerGetDatumsTool(server: McpServer) {
  server.tool(
    "get_datums",
    "Get Revit datum elements such as grids, levels, and reference planes. Coordinates are returned in Revit internal feet; level elevations also include millimeters.",
    {
      datumType: z
        .enum(["all", "grid", "level", "referencePlane"])
        .default("all")
        .describe("Datum type to return."),
      name: z
        .string()
        .optional()
        .describe("Optional exact datum name filter, case-insensitive."),
      activeViewOnly: z
        .boolean()
        .default(false)
        .describe("Only return datums visible in the active view when supported."),
    },
    async (args, extra) => {
      const params = {
        datumType: args.datumType,
        name: args.name,
        activeViewOnly: args.activeViewOnly,
      };

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("get_datums", params);
        });

        return {
          content: [
            {
              type: "text",
              text: JSON.stringify(response, null, 2),
            },
          ],
        };
      } catch (error) {
        return {
          content: [
            {
              type: "text",
              text: `get datums failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
