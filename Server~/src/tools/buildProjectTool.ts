import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

const toolName = 'build_project';
const toolDescription = 'Build the Unity project for a target platform.';

const paramsSchema = z.object({
  target: z
    .string()
    .optional()
    .describe('Build target platform (e.g. "Windows", "Mac", "Linux", "WebGL", "Android", "iOS"). Defaults to the active build target.'),
  outputPath: z
    .string()
    .optional()
    .describe('Output directory for the build. Defaults to Builds/{target}/ in the project root.'),
});

export function registerBuildProjectTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
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
      response.message || 'Failed to build project'
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || 'Successfully built project'
    }]
  };
}
