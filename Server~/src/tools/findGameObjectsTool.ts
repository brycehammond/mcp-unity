import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

const toolName = 'find_gameobjects';
const toolDescription = 'Find GameObjects in the active scene by name pattern, tag, or component type.';

const paramsSchema = z.object({
  namePattern: z.string().optional().describe('Substring or glob pattern (* and ?) to match against GameObject names'),
  tag: z.string().optional().describe('Unity tag to filter by'),
  componentType: z.string().optional().describe('Component type name to filter by, e.g. "Rigidbody"'),
  includeInactive: z.boolean().optional().default(false).describe('Include inactive GameObjects (default false)'),
  maxResults: z.number().int().min(1).max(500).optional().default(50).describe('Maximum number of results (default 50, max 500)')
});

async function toolHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  const response = await mcpUnity.sendRequest({ method: toolName, params });
  if (!response.success) {
    throw new McpUnityError(ErrorType.TOOL_EXECUTION, response.message || 'Failed to find GameObjects');
  }
  return {
    content: [{
      type: 'text',
      text: JSON.stringify(response, null, 2)
    }]
  };
}

export function registerFindGameObjectsTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${toolName}`);
  server.tool(toolName, toolDescription, paramsSchema.shape, async (params: any) => {
    try {
      logger.info(`Executing tool: ${toolName}`, params);
      const result = await toolHandler(mcpUnity, params);
      logger.info(`Tool execution successful: ${toolName}`);
      return result;
    } catch (error) {
      logger.error(`Tool execution failed: ${toolName}`, error);
      throw error;
    }
  });
}
