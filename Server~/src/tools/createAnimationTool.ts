import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

const toolName = 'create_animation';
const toolDescription = 'Create a keyframe animation clip. Supports position, rotation, scale, and color properties with friendly names.';

const Vector3Value = z.object({
  x: z.number(),
  y: z.number(),
  z: z.number(),
});

const ColorValue = z.object({
  r: z.number(),
  g: z.number(),
  b: z.number(),
  a: z.number().optional().default(1),
});

const KeyframeSchema = z.object({
  time: z.number().describe('Time in seconds'),
  value: z.union([Vector3Value, ColorValue]).describe('Keyframe value — {x,y,z} for transform properties, {r,g,b,a} for color'),
});

const AnimPropertySchema = z.object({
  property: z
    .enum(['position', 'rotation', 'scale', 'color'])
    .describe('The property to animate'),
  keyframes: z
    .array(KeyframeSchema)
    .min(1)
    .describe('Array of keyframes with time and value'),
});

const paramsSchema = z.object({
  name: z.string().describe('Name for the animation clip, e.g. "Bounce"'),
  gameObjectName: z
    .string()
    .optional()
    .describe('Attach the clip to this GameObject (optional — if omitted, just creates the clip asset)'),
  loop: z
    .boolean()
    .optional()
    .default(true)
    .describe('Whether the animation loops (default true)'),
  properties: z
    .array(AnimPropertySchema)
    .min(1)
    .describe('Properties to animate with their keyframes'),
});

export function registerCreateAnimationTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
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
      response.message || 'Failed to create animation'
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || 'Successfully created animation'
    }]
  };
}
