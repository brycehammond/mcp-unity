import * as z from 'zod';
import { createWriteStream } from 'fs';
import { mkdir } from 'fs/promises';
import { tmpdir } from 'os';
import { join, basename, extname } from 'path';
import { pipeline } from 'stream/promises';
import { Readable } from 'stream';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

const toolName = 'import_asset';
const toolDescription = 'Download a file from a URL and import it into the Unity project.';

const TEMP_DIR = join(tmpdir(), 'kiln-imports');

const paramsSchema = z.object({
  url: z.string().url().describe('Direct download URL for the asset file'),
  targetDirectory: z
    .string()
    .optional()
    .default('Assets/Imports')
    .describe('Asset directory inside the Unity project'),
  fileName: z
    .string()
    .optional()
    .describe('File name for the imported asset. Derived from the URL if omitted.'),
});

/**
 * Download a URL to a local temp file and return the path.
 */
async function downloadToTemp(url: string, fileName: string): Promise<string> {
  await mkdir(TEMP_DIR, { recursive: true });
  const destPath = join(TEMP_DIR, fileName);

  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`Download failed: HTTP ${response.status} ${response.statusText}`);
  }
  if (!response.body) {
    throw new Error('Download failed: empty response body');
  }

  const nodeStream = Readable.fromWeb(response.body as import('stream/web').ReadableStream);
  const fileStream = createWriteStream(destPath);
  await pipeline(nodeStream, fileStream);

  return destPath;
}

/**
 * Derive a file name from a URL if one wasn't provided.
 */
function fileNameFromUrl(url: string): string {
  try {
    const pathname = new URL(url).pathname;
    const name = basename(pathname);
    if (name && extname(name)) return name;
  } catch {
    // ignore
  }
  return `imported-asset-${Date.now()}`;
}

export function registerImportAssetTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${toolName}`);

  server.tool(
    toolName,
    toolDescription,
    paramsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${toolName}`, params);
        const result = await toolHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${toolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${toolName}`, error);
        throw error;
      }
    }
  );
}

async function toolHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  const { url, targetDirectory, fileName: userFileName } = params;

  const resolvedFileName = userFileName ?? fileNameFromUrl(url);
  const sourcePath = await downloadToTemp(url, resolvedFileName);

  const response = await mcpUnity.sendRequest({
    method: toolName,
    params: {
      sourcePath,
      targetDirectory,
      fileName: resolvedFileName,
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to import asset'
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || 'Successfully imported asset'
    }]
  };
}
