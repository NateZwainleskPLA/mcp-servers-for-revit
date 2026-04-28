import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerMoveElementsTool(server: McpServer) {
  server.tool(
    "move_elements",
    "Move or copy Revit elements by a translation vector. Translation coordinates are in millimeters.",
    {
      elementIds: z.array(z.string()).describe("Element IDs to move or copy."),
      translation: z
        .object({
          x: z.number().default(0).describe("X translation in millimeters."),
          y: z.number().default(0).describe("Y translation in millimeters."),
          z: z.number().default(0).describe("Z translation in millimeters."),
        })
        .describe("Translation vector in millimeters."),
      copyInsteadOfMove: z
        .boolean()
        .default(false)
        .describe("Copy the elements instead of moving the originals."),
    },
    async (args, extra) => {
      const params = {
        elementIds: args.elementIds,
        translation: args.translation,
        copyInsteadOfMove: args.copyInsteadOfMove,
      };

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("move_elements", params);
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
              text: `move elements failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
