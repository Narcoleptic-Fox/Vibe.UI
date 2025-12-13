// Vibe.UI Chart.js Integration
// This file provides JavaScript interop for Chart.js rendering in Blazor

// Chart.js type declarations (minimal subset needed)
declare global {
  interface Window {
    Chart: ChartConstructor;
    vibeChart: VibeChartAPI;
  }
}

// Chart.js constructor interface
interface ChartConstructor {
  new (context: CanvasRenderingContext2D, config: ChartConfig): ChartInstance;
}

// Chart instance interface
interface ChartInstance {
  data: ChartData;
  options: ChartOptions;
  update(): void;
  destroy(): void;
  resize(): void;
  toBase64Image(): string;
  getElementsAtEventForMode(
    event: Event,
    mode: string,
    options: { intersect: boolean },
    useFinalPosition: boolean
  ): ChartElement[];
}

// Chart configuration interface
export interface ChartConfig {
  type?: string;
  data: ChartData;
  options?: ChartOptions;
}

// Chart data interface
export interface ChartData {
  labels?: string[];
  datasets?: Array<{
    label?: string;
    data: number[];
    backgroundColor?: string | string[];
    borderColor?: string | string[];
    borderWidth?: number;
    [key: string]: unknown;
  }>;
  [key: string]: unknown;
}

// Chart options interface
export interface ChartOptions {
  responsive?: boolean;
  maintainAspectRatio?: boolean;
  plugins?: {
    legend?: {
      display?: boolean;
      position?: string;
    };
    title?: {
      display?: boolean;
      text?: string;
    };
    [key: string]: unknown;
  };
  scales?: {
    [key: string]: unknown;
  };
  [key: string]: unknown;
}

// Chart element interface (returned by getElementAtEvent)
export interface ChartElement {
  datasetIndex: number;
  index: number;
  element?: {
    x: number;
    y: number;
    [key: string]: unknown;
  };
  [key: string]: unknown;
}

// Main Vibe Chart API interface
export interface VibeChartAPI {
  charts: Record<string, ChartInstance>;
  createChart(canvasId: string, config: ChartConfig): boolean;
  updateChart(canvasId: string, config: ChartConfig): boolean;
  destroyChart(canvasId: string): boolean;
  resizeChart(canvasId: string): boolean;
  getElementAtEvent(canvasId: string, event: Event): ChartElement | null;
  toBase64Image(canvasId: string): string | null;
}

// Implementation
const vibeChart: VibeChartAPI = {
  charts: {},

  // Initialize or update a chart
  createChart: function (canvasId: string, config: ChartConfig): boolean {
    try {
      // Destroy existing chart if it exists
      if (this.charts[canvasId]) {
        this.charts[canvasId].destroy();
      }

      const canvas = document.getElementById(canvasId);
      if (!canvas) {
        console.error(`Canvas element with id '${canvasId}' not found`);
        return false;
      }

      const ctx = (canvas as HTMLCanvasElement).getContext('2d');
      if (!ctx) {
        console.error(`Failed to get 2D context for canvas '${canvasId}'`);
        return false;
      }

      // Create the chart
      this.charts[canvasId] = new window.Chart(ctx, config);
      return true;
    } catch (error) {
      console.error('Error creating chart:', error);
      return false;
    }
  },

  // Update chart data
  updateChart: function (canvasId: string, config: ChartConfig): boolean {
    try {
      const chart = this.charts[canvasId];
      if (!chart) {
        return this.createChart(canvasId, config);
      }

      // Update chart data
      chart.data = config.data;
      if (config.options) {
        chart.options = config.options;
      }
      chart.update();
      return true;
    } catch (error) {
      console.error('Error updating chart:', error);
      return false;
    }
  },

  // Destroy a chart
  destroyChart: function (canvasId: string): boolean {
    try {
      if (this.charts[canvasId]) {
        this.charts[canvasId].destroy();
        delete this.charts[canvasId];
        return true;
      }
      return false;
    } catch (error) {
      console.error('Error destroying chart:', error);
      return false;
    }
  },

  // Resize a chart
  resizeChart: function (canvasId: string): boolean {
    try {
      const chart = this.charts[canvasId];
      if (chart) {
        chart.resize();
        return true;
      }
      return false;
    } catch (error) {
      console.error('Error resizing chart:', error);
      return false;
    }
  },

  // Get chart data at a specific point
  getElementAtEvent: function (canvasId: string, event: Event): ChartElement | null {
    try {
      const chart = this.charts[canvasId];
      if (chart) {
        const elements = chart.getElementsAtEventForMode(
          event,
          'nearest',
          { intersect: true },
          false
        );
        return elements.length > 0 ? (elements[0] ?? null) : null;
      }
      return null;
    } catch (error) {
      console.error('Error getting element at event:', error);
      return null;
    }
  },

  // Export chart as image
  toBase64Image: function (canvasId: string): string | null {
    try {
      const chart = this.charts[canvasId];
      if (chart) {
        return chart.toBase64Image();
      }
      return null;
    } catch (error) {
      console.error('Error exporting chart:', error);
      return null;
    }
  }
};

// Expose to window for Blazor interop
window.vibeChart = vibeChart;

// Export for testing
export default vibeChart;
