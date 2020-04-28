### DDL

```sql
create schema rent collate latin1_swedish_ci;

create table admin
(
	id int auto_increment
		primary key,
	email varchar(32) not null,
	pwd varchar(32) not null,
	username varchar(10) null,
	sex enum('男', '女') null,
	cardNum varchar(18) null,
	phone varchar(11) null,
	native varchar(10) null,
	major varchar(10) null,
	avatar varchar(100) null,
	createdAt date null
)
comment '管理员表' charset=utf8;

create table applytenant
(
	id int auto_increment
		primary key,
	status enum('待处理', '已处理') default '待处理' null,
	parentNum int null
)
comment '租户续约申请' charset=utf8;

create table entrust
(
	id int auto_increment
		primary key,
	name varchar(10) null,
	phone varchar(11) null,
	city varchar(5) null,
	community varchar(20) null,
	accepted tinyint(1) default 0 null
)
comment '委托申请' charset=utf8;

create table housecollect
(
	id int auto_increment
		primary key,
	houseId varchar(10) null,
	email varchar(32) null,
	createdAt date null
)
comment '已收藏房源';

create table houseorder
(
	id int auto_increment
		primary key,
	houseId int null,
	email varchar(32) null,
	orderTime date null,
	status enum('待完成', '已完成') default '待完成' null,
	orderPhone varchar(11) null,
	adminName varchar(10) null
)
comment '约看房源记录' charset=utf8;

create table houseresource
(
	id int auto_increment
		primary key,
	resType enum('img', 'video', '3d') null,
	resPath varchar(100) null
)
comment '房源展示资源';

create table owner
(
	id int auto_increment
		primary key,
	email varchar(32) null,
	pwd varchar(32) null,
	username varchar(10) null,
	sex enum('男', '女') null,
	cardNum varchar(18) null,
	phone varchar(11) null,
	native varchar(10) null,
	job varchar(10) null,
	createdAt date null,
	userType varchar(2) default '房主' null
)
comment '房主' charset=utf8;

create table house
(
	id int auto_increment
		primary key,
	address varchar(50) null,
	price decimal(6,2) null,
	area float null,
	rentType enum('整租', '合租') null,
	floor varchar(10) null,
	layout varchar(10) null,
	orientation varchar(10) null,
	buildTime varchar(10) null,
	intro text null,
	community varchar(20) null,
	cover varchar(100) null,
	owner int null,
	firstRent tinyint(1) default 1 null,
	status enum('未激活', '空闲', '已租用') default '未激活' null,
	payMethod enum('月付价', '季付价', '年付价') default '月付价' null,
	houseNum varchar(10) null,
	roomNum varchar(10) null,
	constraint house_owner_id_fk
		foreign key (owner) references owner (id)
)
comment '房源' charset=utf8;

create table ownercontract
(
	id int auto_increment
		primary key,
	houseId int null,
	userId int null,
	startAt date null,
	endAt date null,
	contractPic varchar(100) null,
	contractStatus enum('未到期', '已到期') default '未到期' null,
	parentNum int null,
	adminId int null,
	constraint ownercontract_admin_id_fk
		foreign key (adminId) references admin (id),
	constraint ownercontract_house_id_fk
		foreign key (houseId) references house (id),
	constraint ownercontract_owner_id_fk
		foreign key (userId) references owner (id)
)
comment '房主合同' charset=utf8;

create table payment
(
	id int auto_increment
		primary key,
	contractId int null,
	account int null,
	date date null,
	status enum('已支付', '待支付') null,
	admin varchar(10) null,
	constraint payment_ownercontract_id_fk
		foreign key (contractId) references ownercontract (id)
)
comment '付款房主' charset=utf8;

create table selfdesc
(
	id int auto_increment
		primary key,
	email varchar(32) null,
	tag varchar(10) null
)
comment '自我描述' charset=utf8;

create table tenant
(
	id int auto_increment
		primary key,
	email varchar(32) null,
	pwd varchar(32) null,
	username varchar(10) null,
	sex enum('男', '女') null,
	cardNum varchar(18) null,
	phone varchar(11) null,
	native varchar(10) null,
	job varchar(10) null,
	createdAt date null,
	userType varchar(2) default '租户' null
)
comment '租户' charset=utf8;

create table usertype
(
	id int not null
		primary key,
	typeName varchar(6) null
)
comment '用户类型' charset=utf8;

create table user
(
	id int auto_increment
		primary key,
	email varchar(32) null,
	pwd varchar(32) null,
	name varchar(10) null,
	sex enum('男', '女') null,
	cardNum varchar(18) null,
	phone varchar(11) null,
	native varchar(10) null,
	job varchar(10) null,
	createdAt date null,
	type int default 1 null,
	avatar varchar(100) null,
	constraint user_usertype_id_fk
		foreign key (type) references usertype (id)
)
comment '用户' charset=utf8;

create table tenantcontract
(
	id int auto_increment
		primary key,
	houseId int null,
	userId int null,
	startAt date null,
	endAt date null,
	contractPic varchar(100) charset latin1 null,
	contractStatus enum('未到期', '已到期') default '未到期' null,
	parentNum int null,
	adminId int null,
	constraint tenantcontract_house_id_fk
		foreign key (houseId) references house (id),
	constraint tenantcontract_user_id_fk
		foreign key (userId) references user (id)
)
comment '租户合同' charset=utf8;


```