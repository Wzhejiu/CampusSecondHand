<template>
  <div class="admin-layout">
    <el-container>
      <el-header class="admin-header">
        <div class="header-content">
          <div class="header-left">
            <div class="logo">
              <i class="el-icon-s-platform"></i>
              <router-link to="/platform-admin">校园二手交易平台</router-link>
            </div>
          </div>
          <div class="header-right">
            <div class="admin-info">
              <el-avatar size="small" :src="require('../../assets/gou.jpg')"></el-avatar>
              <span class="admin-name">{{admin.nickname}}</span>
            </div>
            <el-button type="danger" size="small" @click="logout">
              <i class="el-icon-switch-button"></i> 退出登录
            </el-button>
          </div>
        </div>
      </el-header>
      <el-container class="main-container">
        <el-aside width="220px" class="admin-aside">
          <el-menu
              default-active="1"
              class="side-menu"
              @select="handleSelect"
              background-color="#304156"
              text-color="#bfcbd9"
              active-text-color="#409EFF">
            <el-menu-item index="1">
              <i class="el-icon-s-check"></i>
              <span slot="title">商品审核</span>
            </el-menu-item>
            <el-menu-item index="2">
              <i class="el-icon-goods"></i>
              <span slot="title">商品管理</span>
            </el-menu-item>

            <el-menu-item index="3">
              <i class="el-icon-user"></i>
              <span slot="title">用户管理</span>
            </el-menu-item>
            <el-menu-item index="4">
              <i class="el-icon-menu"></i>
              <span slot="title">分类管理</span>
            </el-menu-item>
          </el-menu>
        </el-aside>
        <el-main class="admin-main">
          <GoodsAudit v-if="mode == 1"></GoodsAudit>
          <IdleGoods v-if="mode == 2"></IdleGoods>
          <UserManage v-if="mode == 3"></UserManage>
          <CategoryManage v-if="mode == 4"></CategoryManage>
        </el-main>
      </el-container>
    </el-container>
  </div>
</template>

<script>
import GoodsAudit from '../common/GoodsAudit.vue'
import IdleGoods from '../common/IdleGoods.vue'

import UserManage from '../common/UserManage.vue'
import CategoryManage from '../common/CategoryManage.vue'

export default {
  name: "platform-admin",
  components: {
    GoodsAudit,
    IdleGoods,
    UserManage,
    CategoryManage,
  },
  data() {
    return {
      mode: 1,
      admin: {
        nickname: '管理员',
      },
    }
  },
  created() {
    this.admin.nickname = this.$sta.adminName;
  },
  methods: {
    logout() {
      this.$api.loginOut({}).then(res => {
        if (res.status_code === 1) {
          this.$sta.isLogin = false;
          this.$sta.adminName = '';
          this.$router.push({path: '/login-admin'});
        }
      }).catch(e => {
        console.log(e)
      })
    },
    handleSelect(val) {
      if (this.mode !== val) {
        this.mode = val
      }
    }
  },
}
</script>

<style scoped>
.admin-layout {
  min-height: 100vh;
  background-color: #f0f2f5;
}

.admin-header {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 0;
  height: 60px !important;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  z-index: 1000;
}

.header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  height: 100%;
  padding: 0 20px;
}

.header-left .logo {
  display: flex;
  align-items: center;
  gap: 10px;
}

.header-left .logo i {
  font-size: 24px;
  color: #fff;
}

.header-left .logo a {
  color: #fff;
  font-size: 20px;
  font-weight: 700;
  text-decoration: none;
  letter-spacing: 1px;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 20px;
}

.admin-info {
  display: flex;
  align-items: center;
  gap: 8px;
  color: #fff;
}

.admin-name {
  font-size: 14px;
}

.main-container {
  min-height: calc(100vh - 60px);
}

.admin-aside {
  background-color: #304156;
  overflow-y: auto;
  box-shadow: 2px 0 6px rgba(0, 0, 0, 0.1);
}

.side-menu {
  border-right: none;
}

.side-menu .el-menu-item {
  height: 50px;
  line-height: 50px;
  font-size: 14px;
}

.side-menu .el-menu-item:hover {
  background-color: #263445 !important;
}

.side-menu .el-menu-item.is-active {
  background-color: #263445 !important;
}

.admin-main {
  background-color: #f0f2f5;
  padding: 20px;
  min-height: calc(100vh - 60px);
}
</style>