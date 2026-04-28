import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const pointSchema = z.object({
  x: z.number().describe("X coordinate in millimeters."),
  y: z.number().describe("Y coordinate in millimeters."),
  z: z.number().default(0).describe("Z coordinate in millimeters."),
});

export function registerCreateDetailLinesTool(server: McpServer) {
  server.tool(
    "create_detail_lines",
    "Create one or more detail lines in the active Revit view from start and end points. Coordinates are in millimeters.",
    {
      lines: z
        .array(
          z.object({
            start: pointSchema.describe("Line start point in millimeters."),
            end: pointSchema.describe("Line end point in millimeters."),
          })
        )
        .min(1)
        .describe("Detail lines to create in the active view."),
    },
    async (args, extra) => {
      const params = {
        lines: args.lines,
      };

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_detail_lines", params);
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
              text: `create detail lines failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
