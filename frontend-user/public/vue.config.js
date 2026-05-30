module.exports = {
  devServer: {
    port: 8080,
    proxy: {
      '/api': {
        target: 'http://172.19.229.149:5000',
        changeOrigin: true,
        // 重要：因为后端接口路径就是 /api/Category，所以不需要重写路径
        // pathRewrite: { '^/api': '' }  // 不要加这一行！
      }
    }
  }
}