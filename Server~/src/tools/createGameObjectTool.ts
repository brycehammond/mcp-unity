import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

const toolName = 'create_gameobject';
const toolDescription = 'Create a new GameObject in the scene (supports 3D primitives, 2D sprites, components, and colors)';

const Vector3Schema = z.object({
  x: z.number(),
  y: z.number(),
  z: z.number(),
});

const ComponentSpecSchema = z.object({
  type: z.string(),
  properties: z.record(z.unknown()).optional(),
});

const paramsSchema = z.object({
  name: z.string().describe('Name for the new GameObject'),
  primitiveType: z
    .enum(['Cube', 'Sphere', 'Cylinder', 'Capsule', 'Plane', 'Quad', 'Sprite'])
    .optional()
    .describe('Primitive type: 3D mesh (Cube, Sphere, etc.) or 2D Sprite'),
  position: Vector3Schema.optional().describe('World-space position'),
  rotation: Vector3Schema.optional().describe('Euler rotation in degrees'),
  scale: Vector3Schema.optional().describe('Local scale'),
  color: z
    .string()
    .optional()
    .describe('Named color ("red") or hex ("#FF0000")'),
  parentPath: z
    .string()
    .optional()
    .describe('Hierarchy path to parent, e.g. "Environment/Walls"'),
  components: z
    .array(ComponentSpecSchema)
    .optional()
    .describe('Additional components to attach (e.g. Rigidbody, BoxCollider2D, Rigidbody2D)'),
  sortingLayer: z
    .string()
    .optional()
    .describe('Sorting layer name for 2D rendering order'),
  sortingOrder: z
    .number()
    .optional()
    .describe('Order within the sorting layer for 2D rendering'),
});

export function registerCreateGameObjectTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
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
      response.message || 'Failed to create GameObject'
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || 'Successfully created GameObject'
    }]
  };
}
