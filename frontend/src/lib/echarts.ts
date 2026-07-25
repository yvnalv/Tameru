// Tree-shaken ECharts registration — only the chart types + components the app uses, to keep the
// bundle small. Imported once from main.ts.
import { use } from 'echarts/core';
import { CanvasRenderer } from 'echarts/renderers';
import { BarChart, PieChart } from 'echarts/charts';
import { GridComponent, TooltipComponent } from 'echarts/components';

use([CanvasRenderer, BarChart, PieChart, GridComponent, TooltipComponent]);
