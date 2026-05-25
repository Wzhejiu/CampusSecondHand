import request from './request';

const api = {
    // ========== 管理员认证 ==========
    adminLogin(data) {
        return request({
            url: '/Auth/admin/login',  // 注意：baseURL 已经包含 /api，这里只需要 /Auth/admin/login
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
    getGoods(query) {
        return request({
            url: '/admin/idleList',
            method: 'get',
            params: query
        });
    },
    updateGoods(query) {
        return request({
            url: '/admin/updateIdleStatus',
            method: 'get',
            params: query
        });
    },
    queryIdle(query) {
        return request({
            url: '/admin/queryIdle',
            method: 'get',
            params: query
        });
    },

    // ========== 订单管理 ==========
    getOrderList(query) {
        return request({
            url: '/admin/orderList',
            method: 'get',
            params: query
        });
    },
    deleteOrder(query) {
        return request({
            url: '/admin/deleteOrder',
            method: 'get',
            params: query
        });
    },
    queryOrder(query) {
        return request({
            url: '/admin/queryOrder',
            method: 'get',
            params: query
        });
    },

    // ========== 用户管理 ==========
    getUserData(query) {
        return request({
            url: '/admin/userList',
            method: 'get',
            params: query
        });
    },
    getUserManage(query) {
        return request({
            url: '/admin/list',
            method: 'get',
            params: query
        });
    },
    updateUserStatus(query) {
        return request({
            url: '/admin/updateUserStatus',
            method: 'get',
            params: query
        });
    },
    regAdministrator(data) {
        return request({
            url: '/admin/add',
            method: 'post',
            data: data
        });
    },
    queryUser(query) {
        return request({
            url: '/admin/queryUser',
            method: 'get',
            params: query
        });
    }
};

export default api;