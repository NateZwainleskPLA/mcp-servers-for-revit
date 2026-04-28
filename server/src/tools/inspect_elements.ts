import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerInspectElementsTool(server: McpServer) {
  server.tool(
    "inspect_elements",
    "Inspect Revit elements by ID or current selection. Returns identity, category, class, location, optional bounding boxes, optional curve geometry, and selected parameter values. Coordinates are returned in Revit internal feet.",
    {
      elementIds: z
        .array(z.string())
        .optional()
        .describe("Element IDs to inspect. Omit when useSelection is true."),
      useSelection: z
        .boolean()
        .default(false)
        .describe("Inspect the current Revit selection instead of explicit element IDs."),
      limit: z
        .number()
        .int()
        .positive()
        .optional()
        .describe("Maximum number of elements to return."),
      includeGeometry: z
        .boolean()
        .default(false)
        .describe("Include basic curve geometry for curve/detail elements."),
      includeBoundingBox: z
        .boolean()
        .default(false)
        .describe("Include each element bounding box when available."),
      parameterNames: z
        .array(z.string())
        .optional()
        .describe("Parameter names to read from each element."),
    },
    async (args, extra) => {
      const params = {
        elementIds: args.elementIds || [],
        useSelection: args.useSelection,
        limit: args.limit,
        includeGeometry: args.includeGeometry,
        includeBoundingBox: args.includeBoundingBox,
        parameterNames: args.parameterNames || [],
      };

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("inspect_elements", params);
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
              text: `inspect elements failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
