import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

const toolName = 'read_script';
const toolDescription = 'Read the contents of a C# script from the Unity project by path or class name. At least one of path or className must be provided.';

const paramsSchema = z.object({
  path: z
    .string()
    .optional()
    .describe('Asset path, e.g. "Assets/Scripts/MyScript.cs"'),
  className: z
    .string()
    .optional()
    .describe('Find the script by its class name'),
});

export function registerReadScriptTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${toolName}`);

  server.tool(
    toolName,
    toolDescription,
    paramsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${toolName}`, params);

        if (!params.path && !params.className) {
          throw new McpUnityError(
            ErrorType.VALIDATION,
            'You must provide at least one of "path" or "className".'
          );
        }

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

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to read script'
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || 'Successfully read script'
    }]
  };
}
