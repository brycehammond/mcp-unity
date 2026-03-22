import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

const toolName = 'create_script';
const toolDescription = 'Create a new C# script in the Unity project, optionally attaching it to a GameObject.';

const paramsSchema = z.object({
  scriptName: z.string().describe('Class name for the new script'),
  scriptType: z
    .enum(['MonoBehaviour', 'ScriptableObject', 'EditorWindow', 'Plain'])
    .optional()
    .default('MonoBehaviour')
    .describe('Base type of the C# script'),
  code: z
    .string()
    .optional()
    .describe('Full C# source code. If omitted a template is generated.'),
  directory: z
    .string()
    .optional()
    .default('Assets/Scripts')
    .describe('Asset directory in which to save the script'),
  attachTo: z
    .string()
    .optional()
    .describe('Name of a GameObject to attach the script to after creation'),
});

export function registerCreateScriptTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
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

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to create script'
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || 'Successfully created script'
    }]
  };
}
