import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

const toolName = 'screenshot';
const toolDescription = 'Capture a screenshot of the Unity Game or Scene view.';

const paramsSchema = z.object({
  view: z
    .enum(['game', 'scene'])
    .optional()
    .default('game')
    .describe('Which view to capture: "game" (default) or "scene"'),
  width: z
    .number()
    .int()
    .min(64)
    .max(768)
    .optional()
    .default(512)
    .describe('Image width in pixels (64–768, default 512)'),
  height: z
    .number()
    .int()
    .min(64)
    .max(768)
    .optional()
    .default(512)
    .describe('Image height in pixels (64–768, default 512)'),
});

export function registerScreenshotTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
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
  const response = await mcpUnity.sendRequest({
    method: toolName,
    params
  });

  if (!response.success || !response.data?.imageBase64) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Screenshot failed'
    );
  }

  const imageBase64 = response.data.imageBase64 as string;
  const mimeType = (response.data.mimeType as string) ?? 'image/png';

  return {
    content: [
      {
        type: 'image' as const,
        data: imageBase64,
        mimeType,
      },
      {
        type: 'text' as const,
        text: response.message || 'Screenshot captured',
      },
    ],
  };
}
