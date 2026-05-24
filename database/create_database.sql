-- 创建数据库
CREATE DATABASE IF NOT EXISTS second_hand_trade DEFAULT CHARACTER SET utf8mb4;
USE second_hand_trade;

-- 用户表
CREATE TABLE `user` (
  `user_id` BIGINT PRIMARY KEY AUTO_INCREMENT COMMENT '用户id',
  `username` VARCHAR(50) NOT NULL COMMENT '用户名',
  `password` VARCHAR(100) NOT NULL COMMENT '密码',
  `phone` VARCHAR(20) DEFAULT NULL COMMENT '电话',
  `role` TINYINT NOT NULL DEFAULT 0 COMMENT '角色:0-普通用户,1-管理员',
  `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `update_time` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='用户表';

-- 商品分类表
CREATE TABLE `category` (
  `category_id` BIGINT PRIMARY KEY AUTO_INCREMENT COMMENT '商品分类id',
  `category_name` VARCHAR(50) NOT NULL COMMENT '类目名',
  `parent_id` BIGINT DEFAULT NULL COMMENT '父分类id',
  FOREIGN KEY (`parent_id`) REFERENCES `category`(`category_id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='商品分类表';

-- 商品表
CREATE TABLE `goods` (
  `goods_id` BIGINT PRIMARY KEY AUTO_INCREMENT COMMENT '商品id',
  `user_id` BIGINT NOT NULL COMMENT '发布者id',
  `category_id` BIGINT NOT NULL COMMENT '分类id',
  `title` VARCHAR(100) NOT NULL COMMENT '商品名称',
  `description` TEXT DEFAULT NULL COMMENT '商品描述',
  `price` DECIMAL(10,2) NOT NULL COMMENT '价格',
  `audit_status` TINYINT NOT NULL DEFAULT 0 COMMENT '商品状态:0-待审核,1-通过,2-驳回',
  `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `update_time` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  FOREIGN KEY (`user_id`) REFERENCES `user`(`user_id`),
  FOREIGN KEY (`category_id`) REFERENCES `category`(`category_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='商品表';

-- 商品图片表
CREATE TABLE `goods_image` (
  `image_id` BIGINT PRIMARY KEY AUTO_INCREMENT COMMENT '图片id',
  `goods_id` BIGINT NOT NULL COMMENT '商品id',
  `image_url` VARCHAR(255) NOT NULL COMMENT '图片url',
  FOREIGN KEY (`goods_id`) REFERENCES `goods`(`goods_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='商品图片表';

-- 交易表
CREATE TABLE `trade` (
  `trade_id` BIGINT PRIMARY KEY AUTO_INCREMENT COMMENT '交易id',
  `goods_id` BIGINT NOT NULL COMMENT '商品id',
  `buyer_id` BIGINT NOT NULL COMMENT '买家id',
  `seller_id` BIGINT NOT NULL COMMENT '卖家id',
  `meet_time` DATETIME DEFAULT NULL COMMENT '预约时间',
  `meet_location` VARCHAR(255) DEFAULT NULL COMMENT '预约地点',
  `status` TINYINT NOT NULL DEFAULT 0 COMMENT '交易状态:0-待确认,1-已完成,2-取消',
  `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `update_time` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  FOREIGN KEY (`goods_id`) REFERENCES `goods`(`goods_id`),
  FOREIGN KEY (`buyer_id`) REFERENCES `user`(`user_id`),
  FOREIGN KEY (`seller_id`) REFERENCES `user`(`user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='交易表';