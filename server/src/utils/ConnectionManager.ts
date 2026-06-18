import { RevitClientConnection } from "./SocketClient.js";

const LEGACY_PORT = 8080;
const DEFAULT_HOST = "127.0.0.1";

export interface RevitPluginStatus {
  plugin?: string;
  pluginVersion?: string;
  revitVersion?: string;
  port?: number;
  isRunning?: boolean;
  processId?: number;
  activeDocumentTitle?: string | null;
  loadedCommandCount?: number;
}

export interface RevitConnectionConfig {
  host: string;
  requestedVersion?: string;
  explicitPort?: number;
  primaryPort: number;
  candidatePorts: number[];
}

export interface RevitConnectionDiagnostics {
  config: RevitConnectionConfig;
  connected: boolean;
  connectedPort?: number;
  legacyFallbackActive: boolean;
  statusProbeSupported: boolean;
  status?: RevitPluginStatus;
  warnings: string[];
  error?: string;
}

interface ConnectedClient {
  client: RevitClientConnection;
  port: number;
  legacyFallbackActive: boolean;
  statusProbeSupported: boolean;
  status?: RevitPluginStatus;
  warnings: string[];
}

// Mutex to serialize all Revit connections - prevents race conditions
// when multiple requests are made in parallel.
let connectionMutex: Promise<void> = Promise.resolve();
let lastDiagnostics: RevitConnectionDiagnostics | undefined;
const emittedWarnings = new Set<string>();

export function getRevitConnectionConfig(): RevitConnectionConfig {
  const requestedVersion = readOption("revit-version", "REVIT_VERSION");
  const explicitPort = readNumberOption("port", "REVIT_PORT");
  const host = readOption("host", "REVIT_HOST") ?? DEFAULT_HOST;

  if (explicitPort !== undefined) {
    return {
      host,
      requestedVersion,
      explicitPort,
      primaryPort: explicitPort,
      candidatePorts: [explicitPort],
    };
  }

  if (requestedVersion !== undefined) {
    const primaryPort = getDefaultPortForVersion(requestedVersion);
    return {
      host,
      requestedVersion,
      primaryPort,
      candidatePorts: primaryPort === LEGACY_PORT ? [primaryPort] : [primaryPort, LEGACY_PORT],
    };
  }

  return {
    host,
    primaryPort: LEGACY_PORT,
    candidatePorts: [LEGACY_PORT],
  };
}

export function getLastRevitConnectionDiagnostics(): RevitConnectionDiagnostics | undefined {
  return lastDiagnostics;
}

export async function getRevitConnectionDiagnostics(): Promise<RevitConnectionDiagnostics> {
  const config = getRevitConnectionConfig();
  let connectedClient: ConnectedClient | undefined;

  try {
    connectedClient = await connectToConfiguredRevit(config);
    return updateLastDiagnostics({
      config,
      connected: true,
      connectedPort: connectedClient.port,
      legacyFallbackActive: connectedClient.legacyFallbackActive,
      statusProbeSupported: connectedClient.statusProbeSupported,
      status: connectedClient.status,
      warnings: connectedClient.warnings,
    });
  } catch (error) {
    return updateLastDiagnostics({
      config,
      connected: false,
      legacyFallbackActive: false,
      statusProbeSupported: false,
      warnings: [],
      error: error instanceof Error ? error.message : String(error),
    });
  } finally {
    connectedClient?.client.disconnect();
  }
}

/**
 * Connects to Revit and executes the supplied operation.
 */
export async function withRevitConnection<T>(
  operation: (client: RevitClientConnection) => Promise<T>
): Promise<T> {
  const previousMutex = connectionMutex;
  let releaseMutex: () => void;
  connectionMutex = new Promise<void>((resolve) => {
    releaseMutex = resolve;
  });
  await previousMutex;

  const config = getRevitConnectionConfig();
  let connectedClient: ConnectedClient | undefined;

  try {
    connectedClient = await connectToConfiguredRevit(config);
    updateLastDiagnostics({
      config,
      connected: true,
      connectedPort: connectedClient.port,
      legacyFallbackActive: connectedClient.legacyFallbackActive,
      statusProbeSupported: connectedClient.statusProbeSupported,
      status: connectedClient.status,
      warnings: connectedClient.warnings,
    });

    return await operation(connectedClient.client);
  } finally {
    connectedClient?.client.disconnect();
    releaseMutex!();
  }
}

async function connectToConfiguredRevit(
  config: RevitConnectionConfig
): Promise<ConnectedClient> {
  const errors: string[] = [];

  for (const port of config.candidatePorts) {
    const client = new RevitClientConnection(config.host, port);

    try {
      await connectClient(client);
      const statusResult = await probeStatus(client);
      const legacyFallbackActive = port === LEGACY_PORT && config.primaryPort !== LEGACY_PORT;
      const warnings = buildWarnings(config, port, legacyFallbackActive, statusResult);

      if (
        config.requestedVersion !== undefined &&
        statusResult.supported &&
        statusResult.status?.revitVersion !== undefined &&
        statusResult.status.revitVersion !== config.requestedVersion
      ) {
        client.disconnect();
        throw new Error(
          `Connected to Revit ${statusResult.status.revitVersion} on port ${port}, expected Revit ${config.requestedVersion}`
        );
      }

      emitWarnings(warnings);

      return {
        client,
        port,
        legacyFallbackActive,
        statusProbeSupported: statusResult.supported,
        status: statusResult.status,
        warnings,
      };
    } catch (error) {
      client.disconnect();
      errors.push(
        `port ${port}: ${error instanceof Error ? error.message : String(error)}`
      );
    }
  }

  throw new Error(`Failed to connect to Revit. Tried ${errors.join("; ")}`);
}

function connectClient(client: RevitClientConnection): Promise<void> {
  if (client.isConnected) {
    return Promise.resolve();
  }

  return new Promise<void>((resolve, reject) => {
    let settled = false;

    const cleanup = () => {
      clearTimeout(timeout);
      client.socket.removeListener("connect", onConnect);
      client.socket.removeListener("error", onError);
    };

    const onConnect = () => {
      if (settled) return;
      settled = true;
      cleanup();
      resolve();
    };

    const onError = (error: Error) => {
      if (settled) return;
      settled = true;
      cleanup();
      reject(new Error(error.message || "connect to Revit client failed"));
    };

    const timeout = setTimeout(() => {
      if (settled) return;
      settled = true;
      cleanup();
      reject(new Error("Failed to connect to the Revit client"));
    }, 5000);

    client.socket.on("connect", onConnect);
    client.socket.on("error", onError);
    client.connect();
  });
}

async function probeStatus(
  client: RevitClientConnection
): Promise<{ supported: boolean; status?: RevitPluginStatus }> {
  try {
    const status = (await client.sendCommand("mcp_status", {})) as RevitPluginStatus;
    return { supported: true, status };
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    if (message.includes("Method 'mcp_status' not found")) {
      return { supported: false };
    }

    throw error;
  }
}

function buildWarnings(
  config: RevitConnectionConfig,
  connectedPort: number,
  legacyFallbackActive: boolean,
  statusResult: { supported: boolean; status?: RevitPluginStatus }
): string[] {
  const warnings: string[] = [];

  if (legacyFallbackActive) {
    warnings.push(
      `Configured Revit ${config.requestedVersion} target fell back to legacy port ${LEGACY_PORT}. Multi-version routing is unavailable until the Revit plugin is updated.`
    );
  }

  if (config.requestedVersion !== undefined && !statusResult.supported) {
    warnings.push(
      `Connected on port ${connectedPort}, but the Revit plugin does not support mcp_status. Cannot validate that this is Revit ${config.requestedVersion}.`
    );
  }

  if (statusResult.status?.loadedCommandCount === 0) {
    warnings.push(
      `Connected to Revit ${statusResult.status.revitVersion ?? "unknown"}, but the Revit plugin has no command handlers loaded. Revit MCP tools that call model commands will fail until the plugin command registry is loaded.`
    );
  }

  return warnings;
}

function emitWarnings(warnings: string[]): void {
  for (const warning of warnings) {
    if (!emittedWarnings.has(warning)) {
      emittedWarnings.add(warning);
      console.error(`Revit MCP warning: ${warning}`);
    }
  }
}

function updateLastDiagnostics(
  diagnostics: RevitConnectionDiagnostics
): RevitConnectionDiagnostics {
  lastDiagnostics = diagnostics;
  return diagnostics;
}

function readOption(optionName: string, envName: string): string | undefined {
  const argPrefix = `--${optionName}=`;
  const inlineArg = process.argv.find((arg) => arg.startsWith(argPrefix));
  if (inlineArg !== undefined) {
    const value = inlineArg.slice(argPrefix.length).trim();
    return value.length > 0 ? value : undefined;
  }

  const argIndex = process.argv.indexOf(`--${optionName}`);
  if (argIndex >= 0 && process.argv[argIndex + 1] !== undefined) {
    const value = process.argv[argIndex + 1].trim();
    return value.length > 0 ? value : undefined;
  }

  const envValue = process.env[envName]?.trim();
  return envValue && envValue.length > 0 ? envValue : undefined;
}

function readNumberOption(optionName: string, envName: string): number | undefined {
  const value = readOption(optionName, envName);
  if (value === undefined) {
    return undefined;
  }

  const parsed = Number.parseInt(value, 10);
  if (!Number.isInteger(parsed) || parsed < 1 || parsed > 65535) {
    throw new Error(`Invalid ${optionName}: ${value}`);
  }

  return parsed;
}

function getDefaultPortForVersion(revitVersion: string): number {
  const parsed = Number.parseInt(revitVersion, 10);
  if (!Number.isInteger(parsed) || parsed < 2020 || parsed > 2099) {
    throw new Error(`Invalid revit-version: ${revitVersion}`);
  }

  return 39200 + (parsed % 100);
}
