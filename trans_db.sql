create database	trans_db;
use trans_db;

create table users (
    id int auto_increment primary key,
    username varchar(100) unique not null,
    password_hash varchar(255) not null,
    role varchar(20) check (role in ('admin','driver','customer')),
    is_active boolean default true,
    created_at timestamp default current_timestamp
);

create table user_profiles (
    user_id int primary key,
    full_name varchar(255),
    email varchar(255) unique,
    phone varchar(20),
    avatar_url text,
    address text,
    constraint fk_user_profiles_user foreign key (user_id) references users(id)
);


create table trucks (
    id int auto_increment primary key,
    license_plate varchar(50) unique not null,
    brand varchar(100),
    model varchar(100),
    max_weight decimal(10,2) check (max_weight > 0),
    fuel_type varchar(50),
    status varchar(20) check (status in ('available','on_trip','maintenance')),
    current_lat decimal(10,8),
    current_long decimal(11,8)
);

create table drivers (
    id int auto_increment primary key,
    user_id int not null,
    license_number varchar(50) unique not null,
    license_class varchar(10),
    experience_years int check (experience_years >= 0),
    rating_avg decimal(3,2) check (rating_avg >= 0),
    status varchar(20) check (status in ('active','off')),
    constraint fk_drivers_user foreign key (user_id) references users(id)
);

create table truck_driver_assignment (
    id int auto_increment primary key,
    truck_id int not null,
    driver_id int not null,
    assigned_at timestamp default current_timestamp,
    is_primary boolean default true,
    constraint fk_assignment_truck foreign key (truck_id) references trucks(id),
    constraint fk_assignment_driver foreign key (driver_id) references drivers(id)
);


create table locations (
    id int auto_increment primary key,
    name varchar(255) not null,
    address text,
    lat decimal(10,8),
    lng decimal(11,8),
    type varchar(50) check (type in ('warehouse','customer_point'))
);

create table orders (
    id int auto_increment primary key,
    order_code varchar(50) unique not null,
    customer_id int not null,
    pickup_loc_id int not null,
    delivery_loc_id int not null,
    cargo_type varchar(100),
    weight decimal(10,2) check (weight > 0),
    distance_km decimal(10,2) check (distance_km >= 0),
    total_price decimal(12,2) check (total_price >= 0),
    status varchar(20) check (status in ('pending','assigned','picking','in_transit','delivered','cancelled')),
    created_at timestamp default current_timestamp,
    constraint fk_orders_customer foreign key (customer_id) references users(id),
    constraint fk_orders_pickup foreign key (pickup_loc_id) references locations(id),
    constraint fk_orders_delivery foreign key (delivery_loc_id) references locations(id)
);

create table trips (
    id int auto_increment primary key,
    order_id int not null,
    truck_id int not null,
    driver_id int not null,
    start_time timestamp,
    end_time timestamp,
    actual_route_url text,
    constraint fk_trips_order foreign key (order_id) references orders(id),
    constraint fk_trips_truck foreign key (truck_id) references trucks(id),
    constraint fk_trips_driver foreign key (driver_id) references drivers(id)
);


create table order_status_logs (
    id int auto_increment primary key,
    order_id int not null,
    old_status varchar(20),
    new_status varchar(20),
    changed_by_user_id int not null,
    timestamp timestamp default current_timestamp,
    note text,
    constraint fk_logs_order foreign key (order_id) references orders(id),
    constraint fk_logs_user foreign key (changed_by_user_id) references users(id)
);

create table driver_ratings (
    id int auto_increment primary key,
    order_id int not null,
    customer_id int not null,
    driver_id int not null,
    score int check (score between 1 and 5),
    comment text,
    constraint fk_ratings_order foreign key (order_id) references orders(id),
    constraint fk_ratings_customer foreign key (customer_id) references users(id),
    constraint fk_ratings_driver foreign key (driver_id) references drivers(id)
);

create table system_configs (
    config_key varchar(100) primary key,
    config_value varchar(255) not null,
    description text
);


create index idx_users_username on users(username);
create index idx_trucks_license_plate on trucks(license_plate);
create index idx_orders_order_code on orders(order_code);
create index idx_trips_truck_id on trips(truck_id);
create index idx_trips_driver_id on trips(driver_id);
create index idx_locations_name on locations(name);
