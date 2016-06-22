(function ($) {
    if (!$.fn.jqplot) {
        alert("Missing the jqPlot library!");
        return;
    }

    $.fn.reports = function () {
        return this.each(function () {
            var el = $(this),
                data = el.data();

            if (!data.isStackRanking) {
                $.jqplot(el.prop('id'), data.dataRows,
                    $.extend(true /* recursive */, {}, $.fn.reports.defaults, {
                        series: data.dataRowsLabels,
                        axes: {
                            xaxis: {
                                ticks: data.xaxisLabels
                            },
                            yaxis: {
                                max: (data.categoryCount || 1) * 10, /* 10 = max rating per category */
                                tickInterval: (data.categoryCount || 1)
                            }
                        }
                    })
                );
            } else {
                $.jqplot(el.prop('id'), data.dataRows,
                    $.extend(true /* recursive */, {}, $.fn.reports.defaults, {
                        stackSeries: true,
                        captureRightClick: true,
                        pointLabels: {
                            stackSeries: true
                        },
                        series: data.dataRowsLabels,
                        axes: {
                            xaxis: {
                                ticks: data.xaxisLabels
                            },
                            yaxis: {
                                max: data.categoryCount * 10, /* 10 = max rating per category */
                                tickInterval: data.categoryCount
                            }
                        }
                    })
                );
            }
        });
    };

    $.fn.reports.defaults = {
        // The "seriesDefaults" option is an options object that will
        // be applied to all series in the chart.
        seriesDefaults: {
            renderer: $.jqplot.BarRenderer,
            rendererOptions: {
                fillToZero: true,
                barMargin: 12, // number of pixels between adjacent groups of bars.
                barPadding: 0 // number of pixels between adjacent bars in the same
                // group (same category or bin).
            },
            shadow: false,
            pointLabels: {
                show: true,
                location: 's' /* 's' = south, default is 'n'(orth) */,
                hideZeros: true,
                formatString: '%.2p' /* in jquery.jqplot.js, look for: "$.jqplot.sprintf = function()" */
            }
        },
        // Custom peerLabels for the series are specified with the "label"
        // option on the series option.  Here a series option object
        // is specified for each series.
        series: [],
        // Show the legend and put it outside the grid, but inside the
        // plot container, shrinking the grid to accomodate the legend.
        // A value of "outside" would not shrink the grid and allow
        // the legend to overflow the container.
        legend: {
            show: true,
            placement: 'outsideGrid'
        },
        seriesColors: ["#62C462", "#FFD42A", "#007ACC", "#FAA732", "#49AFCD"],
        grid: {
            drawGridLines: true, // wether to draw lines across the grid or not.
            gridLineColor: '#dddddd', // Color of the grid lines.
            background: '#fafafa', // CSS color spec for background color of grid.
            borderColor: '#999999', // CSS color spec for border around grid.
            borderWidth: 0, // pixel width of border around grid.
            shadow: false, // draw a shadow for grid.                        
            renderer: $.jqplot.CanvasGridRenderer, // renderer to use to draw the grid.
            rendererOptions: {} // options to pass to the renderer.  Note, the default
            // CanvasGridRenderer takes no additional options.
        },
        axes: {
            // Use a category axis on the x axis and use our custom ticks.
            xaxis: {
                renderer: $.jqplot.CategoryAxisRenderer,
                ticks: null
            },
            // Pad the y axis just a little so bars can get close to, but
            // not touch, the grid boundaries.  1.2 is the default padding.
            yaxis: {
                pad: 1.05,
                min: 0,
                max: 10,
                tickInterval: 1 /* for grid lines and yaxis labels */
            }
        }
    };
})(jQuery);