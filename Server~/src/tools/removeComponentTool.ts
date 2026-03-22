import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

const toolName = 'remove_component';
const toolDescription = 'Remove a component from a GameObject by type name.';

const paramsSchema = z.object({
  instanceId: z.number().optional().describe('Instance ID of the target GameObject'),
  objectPath: z.string().optional().describe('Hierarchy path of the target GameObject'),
  componentName: z.string().describe('Name of the component type to remove, e.g. "Rigidbody"')
});

async function toolHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  const response = await mcpUnity.sendRequest({ method: toolName, params });
  if (!response.success) {
    throw new McpUnityError(ErrorType.TOOL_EXECUTION, response.message || 'Failed to remove component');
  }
  return { content: [{ type: response.type, text: response.message || 'Successfully removed component' }] };
}

export function registerRemoveComponentTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${toolName}`);
  server.tool(toolName, toolDescription, paramsSchema.shape, async (params: any) => {
    try {
      logger.info(`Executing tool: ${toolName}`, params);

      if (params.instanceId === undefined && !params.objectPath) {
        throw new McpUnityError(ErrorType.VALIDATION, 'Either "instanceId" or "objectPath" must be provided.');
      }

      const result = await toolHandler(mcpUnity, params);
      logger.info(`Tool execution successful: ${toolName}`);
      return result;
    } catch (error) {
      logger.error(`Tool execution failed: ${toolName}`, error);
      throw error;
    }
  });
}
