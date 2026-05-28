import request from './request';

const api = {
    // ========== 管理员认证 ==========
    adminLogin(data) {
        return request({
            url: '/Auth/admin/login',
            method: 'post',
            data: data
        });
    },

    // 管理员注册
    adminRegister(data) {
        return request({
            url: '/Auth/register',
            method: 'post',
            data: data
        });
    },

    loginOut(query) {
        localStorage.removeItem('adminToken');
        localStorage.removeItem('adminInfo');
        return Promise.resolve({
            status_code: 1,
            msg: '退出成功',
            data: null
        });
    },

    // ========== 闲置管理 ==========
    // 获取待审核商品
    getPendingGoods(query) {
        return request({
            url: '/Admin/goods/pending',
            method: 'get',
            params: query
        });
    },
    // 审核商品
    auditGoods(id, data) {
        return request({
            url: `/Admin/goods/${id}/audit`,
            method: 'put',
            data: data
        });
    },
    // 获取所有商品
    getGoods(query) {
        return request({
            url: '/Goods',
            method: 'get',
            params: query
        });
    },
    // 获取我的商品（含审核状态筛选）
    getMyGoods(query) {
        return request({
            url: '/Goods/my',
            method: 'get',
            params: query
        });
    },
    // 更新商品状态
    updateGoodsStatus(id, data) {
        return request({
            url: `/Goods/${id}`,
            method: 'put',
            data: data
        });
    },
    // 删除商品
    deleteGoods(id) {
        return request({
            url: `/Goods/${id}`,
            method: 'delete'
        });
    },
    // 搜索商品
    searchGoods(query) {
        return request({
            url: '/Goods',
            method: 'get',
            params: query
        });
    },

    // ========== 订单管理 ==========
    // 获取订单列表
    getOrderList(query) {
        return request({
            url: '/Trade',
            method: 'get',
            params: query
        });
    },
    // 完成订单
    completeOrder(id) {
        return request({
            url: `/Trade/${id}/complete`,
            method: 'put'
        });
    },
    // 取消订单
    cancelOrder(id) {
        return request({
            url: `/Trade/${id}/cancel`,
            method: 'put'
        });
    },
    // 搜索订单
    searchOrder(query) {
        return request({
            url: '/Trade',
            method: 'get',
            params: query
        });
    },

    // ========== 用户管理 ==========
    // 获取用户列表
    getUserList(query) {
        return request({
            url: '/Admin/users',
            method: 'get',
            params: query
        });
    },
    // 获取用户详情
    getUserDetail(id) {
        return request({
            url: `/Admin/users/${id}`,
            method: 'get'
        });
    },
    // 更新用户角色
    updateUserRole(id, data) {
        return request({
            url: `/Admin/users/${id}/role`,
            method: 'put',
            data: data
        });
    },
    // 搜索用户
    searchUser(query) {
        return request({
            url: '/Admin/users',
            method: 'get',
            params: query
        });
    },
    // 删除用户
    deleteUser(id) {
        return request({
            url: `/Admin/users/${id}`,
            method: 'delete'
        });
    },

    // ========== 分类管理 ==========
    // 获取所有分类
    getCategories() {
        return request({
            url: '/Category',
            method: 'get'
        });
    },
    // 创建分类
    createCategory(data) {
        return request({
            url: '/Category',
            method: 'post',
            data: data
        });
    },
    // 更新分类
    updateCategory(id, data) {
        return request({
            url: `/Category/${id}`,
            method: 'put',
            data: data
        });
    },
    // 删除分类
    deleteCategory(id) {
        return request({
            url: `/Category/${id}`,
            method: 'delete'
        });
    },

    // ========== 文件上传 ==========
    // 单文件上传
    uploadFile(file) {
        const formData = new FormData();
        formData.append('file', file);
        return request({
            url: '/Upload',
            method: 'post',
            data: formData,
            headers: {
                'Content-Type': 'multipart/form-data'
            }
        });
    },
    // 批量文件上传
    uploadFiles(files) {
        const formData = new FormData();
        files.forEach(file => {
            formData.append('files', file);
        });
        return request({
            url: '/Upload/batch',
            method: 'post',
            data: formData,
            headers: {
                'Content-Type': 'multipart/form-data'
            }
        });
    }
};

export default api;