module.exports = {
    publicPath: './',
    assetsDir: 'static',
    productionSourceMap: false,
    configureWebpack: {
        plugins: []
    },
    devServer: {
        port: 8080,
        proxy: {
            '/api': {
                target: 'http://172.19.229.149:5000',
                changeOrigin: true,
                ws: true
            }
        }
    }
}