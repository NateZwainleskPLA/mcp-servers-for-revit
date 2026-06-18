#!/usr/bin/env node
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { registerTools } from "./tools/register.js";
import { getRevitConnectionConfig } from "./utils/ConnectionManager.js";

// 创建服务器实例
const server = new McpServer({
  name: "mcp-server-for-revit",
  version: "1.0.0",
});

// 启动服务器
async function main() {
  const config = getRevitConnectionConfig();
  console.error(
    `Revit MCP target: version=${config.requestedVersion ?? "legacy"} host=${config.host} ports=${config.candidatePorts.join(",")}`
  );

  // 注册工具
  await registerTools(server);

  // 连接到传输层
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error("Revit MCP Server start success");
}

main().catch((error) => {
  console.error("Error starting Revit MCP Server:", error);
  process.exit(1);
});
